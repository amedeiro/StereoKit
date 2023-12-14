/* SPDX-License-Identifier: MIT */
/* The authors below grant copyright rights under the MIT license:
 * Copyright (c) 2019-2023 Nick Klingensmith
 * Copyright (c) 2023 Qualcomm Technologies, Inc.
 */

#include "android.h"
#if defined(SK_OS_ANDROID)

#include "../log.h"
#include "../device.h"
#include "../sk_math.h"
#include "../xr_backends/openxr.h"
#include "../systems/render.h"
#include "../systems/system.h"
#include "../_stereokit.h"
#include "../libraries/sk_gpu.h"
#include "../libraries/sokol_time.h"
#include "../libraries/stref.h"

#include <android/native_activity.h>
#include <android/native_window_jni.h>
#include <android/asset_manager.h>
#include <android/asset_manager_jni.h>
#include <android/font_matcher.h>
#include <android/font.h>

#include <unistd.h>
#include <dlfcn.h>
#include <syslog.h>

namespace sk {

struct window_event_t {
	platform_evt_           type;
	platform_evt_data_t     data;
};

struct window_t {
	array_t<window_event_t> events;
	ANativeWindow*          window;
	skg_swapchain_t         swapchain;
	bool                    has_swapchain;
	bool                    uses_swapchain;
};

JavaVM           *android_vm                = nullptr;
jobject           android_activity          = nullptr;
JNIEnv           *android_env               = nullptr;
AAssetManager    *android_asset_manager     = nullptr;
ANativeWindow    *android_next_window       = nullptr;
jobject           android_next_window_xam   = nullptr;
bool              android_next_win_ready    = false;
bool              android_log_open          = false;
window_t          android_window            = {};

///////////////////////////////////////////

void platform_win_resize(platform_win_t window_id, int32_t width, int32_t height);
void android_log_channel_open ();
void android_log_channel_close();

///////////////////////////////////////////

// When consumed as a static library, the user's app will need to provide its
// own JNI_OnLoad function, which will then conflict with this one. So we only
// provide this for shared libraries.
#if defined(SK_BUILD_SHARED)

extern "C" jint JNI_OnLoad(JavaVM* vm, void* reserved) {
	android_vm = vm;
	return JNI_VERSION_1_6;
}
extern "C" jint JNI_OnLoad_L(JavaVM* vm, void* reserved) {
	return JNI_OnLoad(vm, reserved);
}

#endif

///////////////////////////////////////////

bool platform_impl_init() {
	const sk_settings_t* settings = sk_get_settings_ref();

	// This will re-open on the first log, we're doing this here to make sure the
	// tag gets set correctly, in case log messages were sent before
	// initialization
	android_log_channel_close();

	android_activity   = (jobject)settings->android_activity;
	if (android_vm == nullptr)
		android_vm = (JavaVM*)settings->android_java_vm;

	if (android_vm == nullptr || android_activity == nullptr) {
		log_fail_reason(95, log_error, "Couldn't find Android's Java VM or Activity, you should load the StereoKitC library with something like Xamarin's JavaSystem.LoadLibrary, or manually assign it using sk_set_settings()");
		return false;
	}

	// Get the java environment from the VM, and ensure it's attached to this
	// thread
	int result = android_vm->GetEnv((void **)&android_env, JNI_VERSION_1_6);
	if (result == JNI_EDETACHED) {
		if (android_vm->AttachCurrentThread(&android_env, nullptr) != JNI_OK) {
			log_fail_reason(95, log_error, "Couldn't attach the Java Environment to the current thread!");
			return false;
		}
	} else if (result != JNI_OK) {
		log_fail_reason(95, log_error, "Couldn't get the Java Environment from the VM, this needs to be called from the main thread.");
		return false;
	}

	// https://stackoverflow.com/questions/51099200/native-crash-jni-detected-error-in-application-thread-using-jnienv-from-th
	//android_vm->AttachCurrentThread(&android_env, nullptr);

	// Find the current android activity
	// https://stackoverflow.com/questions/46869901/how-to-get-the-android-context-instance-when-calling-jni-method
	/*jclass    activity_thread      = android_env->FindClass("android/app/ActivityThread");
	jmethodID curr_activity_thread = android_env->GetStaticMethodID(activity_thread, "currentActivityThread", "()Landroid/app/ActivityThread;");
	jobject   at                   = android_env->CallStaticObjectMethod(activity_thread, curr_activity_thread);
	jmethodID get_application      = android_env->GetMethodID(activity_thread, "getApplication", "()Landroid/app/Application;");
	jobject   activity_inst        = android_env->CallObjectMethod(at, get_application);
	android_activity = android_env->NewGlobalRef(activity_inst);
	if (android_activity == nullptr) {
		log_fail_reason(95,  "Couldn't find the current Android application context!");
		return false;
	}*/

	// Get the asset manager for loading files
	// from https://stackoverflow.com/questions/22436259/android-ndk-why-is-aassetmanager-open-returning-null/22436260#22436260
	jclass    activity_class           = android_env->GetObjectClass(android_activity);
	jmethodID activity_class_getAssets = android_env->GetMethodID(activity_class, "getAssets", "()Landroid/content/res/AssetManager;");
	jobject   asset_manager            = android_env->CallObjectMethod(android_activity, activity_class_getAssets); // activity.getAssets();
	jobject   global_asset_manager     = android_env->NewGlobalRef(asset_manager);
	android_asset_manager = AAssetManager_fromJava(android_env, global_asset_manager);
	if (android_asset_manager == nullptr) {
		log_fail_reason(95, log_error, "Couldn't get the Android asset manager!");
		return false;
	}

#if defined(SK_DYNAMIC_OPENXR)
	// Android has no universally supported openxr_loader yet, so on this
	// platform we don't static link it, and instead provide devs a way to ship
	// other loaders.
	if (settings->mode == app_mode_xr && dlopen("libopenxr_loader.so", RTLD_NOW) == nullptr) {
		log_fail_reason(95, log_error, "openxr_loader failed to load!");
		return false;
	}
#endif

	return true;
}

///////////////////////////////////////////

void platform_impl_shutdown() {
	android_log_channel_close();
	if (android_vm)
		android_vm->DetachCurrentThread();
}

///////////////////////////////////////////

void platform_impl_step() {
	if (!android_next_win_ready) return;

	// If we got our window from xamarin, it's a jobject, and needs
	// converted into an ANativeWindow first!
	if (android_next_window_xam != nullptr) {
		android_next_window = ANativeWindow_fromSurface(android_env, android_next_window_xam);
		android_next_window_xam = nullptr;
	}

	if (android_window.window && android_window.window == android_next_window) {
		// It's the same window, lets just resize it
		int32_t width  = ANativeWindow_getWidth (android_window.window);
		int32_t height = ANativeWindow_getHeight(android_window.window);
		platform_win_resize(1, width, height);
	} else {
		// Completely new window! Destroy the old swapchain, and make a
		// new one.
		if (android_window.has_swapchain) {
			android_window.has_swapchain = false;
			skg_swapchain_destroy(&android_window.swapchain);
		}
		android_window.window = (ANativeWindow*)android_next_window;

		if (android_window.window) {
			int32_t width  = ANativeWindow_getWidth (android_window.window);
			int32_t height = ANativeWindow_getHeight(android_window.window);
			platform_win_resize(1, width, height);
		}
	}
	android_next_win_ready = false;
	android_next_window    = nullptr;
}

///////////////////////////////////////////

void platform_win_resize(platform_win_t window_id, int32_t width, int32_t height) {
	if (window_id != 1) return;
	window_t * win = &android_window;

	width  = maxi(1, width);
	height = maxi(1, height);

	window_event_t e = { platform_evt_resize };
	e.data.resize = { width, height };
	win->events.add(e);

	if (win->uses_swapchain == false) return;

	if (win->has_swapchain == false) {
		skg_tex_fmt_ color_fmt = skg_tex_fmt_rgba32_linear;
		skg_tex_fmt_ depth_fmt = (skg_tex_fmt_)render_preferred_depth_fmt();
		win->swapchain     = skg_swapchain_create(win->window, color_fmt, skg_tex_fmt_none, width, height);
		win->has_swapchain = true;

		log_diagf("Created swapchain: %dx%d color:%s depth:%s", win->swapchain.width, win->swapchain.height, render_fmt_name((tex_format_)color_fmt), render_fmt_name((tex_format_)depth_fmt));
	}
	else if (width == win->swapchain.width && height == win->swapchain.height) {
		skg_swapchain_resize(&win->swapchain, width, height);
	}
}

///////////////////////////////////////////

void android_set_window(void *window) {
	android_next_window    = (ANativeWindow*)window;
	android_next_win_ready = true;
}

///////////////////////////////////////////

void android_set_window_xam(void *window) {
	android_next_window_xam = (jobject)window;
	android_next_win_ready  = true;
}

///////////////////////////////////////////

bool android_read_asset(const char* asset_name, void** out_data, size_t* out_size) {
	// See: http://www.50ply.com/blog/2013/01/19/loading-compressed-android-assets-with-file-pointer/

	AAsset *asset = AAssetManager_open(android_asset_manager, asset_name, AASSET_MODE_BUFFER);
	if (asset) {
		*out_size = AAsset_getLength(asset);
		*out_data = sk_malloc(*out_size + 1);
		AAsset_read (asset, *out_data, *out_size);
		AAsset_close(asset);

		((uint8_t *)*out_data)[*out_size] = 0;
		return true;
	}
	return false;
}

///////////////////////////////////////////
// Permissions                           //
///////////////////////////////////////////

const int32_t PERMISSION_DENIED  = -1;
const int32_t PERMISSION_GRANTED = 0;

///////////////////////////////////////////

bool android_check_app_permission(const char* permission) {
	jclass    class_activity               = android_env->GetObjectClass(android_activity);
	jmethodID activity_checkSelfPermission = android_env->GetMethodID   (class_activity, "checkSelfPermission", "(Ljava/lang/String;)I");

	jstring jobj_permission = android_env->NewStringUTF(permission);
	jint    result          = android_env->CallIntMethod(android_activity, activity_checkSelfPermission, jobj_permission);

	android_env->DeleteLocalRef(jobj_permission);

	return result == PERMISSION_GRANTED;
}

///////////////////////////////////////////

bool android_check_manifest_permission(const char* permission)
{
	jclass       class_activity                =          android_env->GetObjectClass  (android_activity);
	jmethodID    activity_getPackageManager    =          android_env->GetMethodID     (class_activity, "getPackageManager", "()Landroid/content/pm/PackageManager;");
	jmethodID    activity_getPackageName       =          android_env->GetMethodID     (class_activity, "getPackageName", "()Ljava/lang/String;");
	jobject      jobj_packageManager           =          android_env->CallObjectMethod(android_activity, activity_getPackageManager);
	jclass       class_packageManager          =          android_env->GetObjectClass  (jobj_packageManager);
	jmethodID    packageManager_getPackageInfo =          android_env->GetMethodID     (class_packageManager, "getPackageInfo", "(Ljava/lang/String;I)Landroid/content/pm/PackageInfo;");
	jstring      jobj_packageName              = (jstring)android_env->CallObjectMethod(android_activity, activity_getPackageName);

	jint         flags = 4096; // PackageManager.GET_PERMISSIONS
	jobject      jobj_packageInfo  = android_env->CallObjectMethod(jobj_packageManager, packageManager_getPackageInfo, jobj_packageName, flags);
	jclass       class_packageInfo = android_env->GetObjectClass(jobj_packageInfo);

	jfieldID     packageInfo_requestedPermissions = android_env->GetFieldID(class_packageInfo, "requestedPermissions", "[Ljava/lang/String;");
	jobjectArray jobj_requestedPermissions        = (jobjectArray)android_env->GetObjectField(jobj_packageInfo, packageInfo_requestedPermissions);

	jstring      jobj_permission = android_env->NewStringUTF(permission);
	jclass       class_string    = android_env->GetObjectClass(jobj_permission);
	jmethodID    string_equals   = android_env->GetMethodID(class_string, "equals", "(Ljava/lang/Object;)Z");

	// Iterate over the array
	jsize length = android_env->GetArrayLength(jobj_requestedPermissions);
	bool  result = false;
	for (jsize i = 0; i < length; i++) {
		jstring jobj_currPermission = (jstring)android_env->GetObjectArrayElement(jobj_requestedPermissions, i);
		bool match = android_env->CallBooleanMethod(jobj_permission, string_equals, jobj_currPermission);
		android_env->DeleteLocalRef(jobj_currPermission);

		if (match) {
			result = true;
			break;
		}
	}

	android_env->DeleteLocalRef(jobj_packageManager);
	android_env->DeleteLocalRef(jobj_packageName);
	android_env->DeleteLocalRef(jobj_packageInfo);
	android_env->DeleteLocalRef(jobj_requestedPermissions);
	android_env->DeleteLocalRef(jobj_permission);

	return result;

}

///////////////////////////////////////////

void android_request_permission(const char* permission) {
	jclass    class_activity                    = android_env->GetObjectClass(android_activity);
	jmethodID contextCompat_checkSelfPermission = android_env->GetMethodID   (class_activity, "checkSelfPermission", "(Ljava/lang/String;)I");
	jmethodID activity_requestPermissions       = android_env->GetMethodID   (class_activity, "requestPermissions",  "([Ljava/lang/String;I)V");

	jstring      jobj_permission      = android_env->NewStringUTF  (permission);
	jobjectArray jobj_permission_list = android_env->NewObjectArray(1, android_env->FindClass("java/lang/String"), NULL);

	android_env->SetObjectArrayElement(jobj_permission_list, 0, jobj_permission);
	android_env->CallVoidMethod       (android_activity, activity_requestPermissions, jobj_permission_list, 0);

	android_env->DeleteLocalRef(jobj_permission);
	android_env->DeleteLocalRef(jobj_permission_list);
}

///////////////////////////////////////////
// Backend                               //
///////////////////////////////////////////

void *backend_android_get_java_vm () { return android_vm; }
void *backend_android_get_activity() { return android_activity; }
void *backend_android_get_jni_env () { return android_env; }

///////////////////////////////////////////
// Window code                           //
///////////////////////////////////////////

platform_win_type_ platform_win_type() { return platform_win_type_existing; }

///////////////////////////////////////////

platform_win_t platform_win_make(const char* title, recti_t win_rect, platform_surface_ surface_type) { return -1; }

///////////////////////////////////////////

platform_win_t platform_win_get_existing(platform_surface_ surface_type) {
	window_t* win = &android_window;

	// Not all windows need a swapchain, but here's where we make 'em for those
	// that do.
	if (surface_type == platform_surface_swapchain) {
		win->uses_swapchain = true;
		if (win->window) {
			int32_t width  = ANativeWindow_getWidth (win->window);
			int32_t height = ANativeWindow_getHeight(win->window);
			platform_win_resize(1, width, height);
		}
	}
	return 1;
}

///////////////////////////////////////////

void platform_win_destroy(platform_win_t window) {
	if (window != 1) return;
	window_t* win = &android_window;

	if (win->has_swapchain) {
		skg_swapchain_destroy(&win->swapchain);
	}

	win->events.free();
	*win = {};
}

///////////////////////////////////////////

void platform_check_events() {
}

///////////////////////////////////////////

bool platform_win_next_event(platform_win_t window_id, platform_evt_* out_event, platform_evt_data_t* out_event_data) {
	if (window_id != 1) return false;
	window_t* win = &android_window;

	if (win->events.count > 0) {
		*out_event      = win->events[0].type;
		*out_event_data = win->events[0].data;
		win->events.remove(0);
		return true;
	} return false;
}

///////////////////////////////////////////

skg_swapchain_t* platform_win_get_swapchain(platform_win_t window) {
	if (window != 1) return nullptr;
	window_t* win = &android_window;

	return win->has_swapchain ? &win->swapchain : nullptr;
}

///////////////////////////////////////////

recti_t platform_win_rect(platform_win_t window_id) {
	if (window_id != 1) return {};
	window_t* win = &android_window;

	return recti_t{ 0, 0,
		win->swapchain.width,
		win->swapchain.height };
}

///////////////////////////////////////////

// TODO: find an alternative to the registry for Android
bool platform_key_save_bytes(const char* key, void* data,       int32_t data_size)   { return false; }
bool platform_key_load_bytes(const char* key, void* ref_buffer, int32_t buffer_size) { return false; }

///////////////////////////////////////////

void platform_msgbox_err(const char* text, const char* header) {
	log_warn("No messagebox capability for this platform!");
}

///////////////////////////////////////////

bool  platform_get_cursor(vec2 *out_pos)   { *out_pos = { 0,0 }; return false; }
void  platform_set_cursor(vec2 window_pos) { }
float platform_get_scroll()                { return 0; }

///////////////////////////////////////////

void platform_xr_keyboard_show   (bool show) { }
bool platform_xr_keyboard_present()          { return false; }
bool platform_xr_keyboard_visible()          { return false; }

///////////////////////////////////////////

font_t platform_default_font() {
	// If we're using Android API 29+, we can just look up the system font!
	array_t<const char *> fonts         = array_t<const char *>::make(2);
	font_t                result        = nullptr;
	const char           *file_latin    = nullptr;
	const char           *file_japanese = nullptr;

#if __ANDROID_API__ >= 29
	AFontMatcher *matcher = AFontMatcher_create();
	AFont *font_latin    = AFontMatcher_match(matcher, "sans-serif", (uint16_t*)u"A", 1, nullptr);
	AFont *font_japanese = AFontMatcher_match(matcher, "sans-serif", (uint16_t*)u"\u3042", 1, nullptr);
	if (font_latin   ) file_latin    = AFont_getFontFilePath(font_latin);
	if (font_japanese) file_japanese = AFont_getFontFilePath(font_japanese);
#endif
	if      (file_latin != nullptr)                                      fonts.add(file_latin);
	else if (platform_file_exists("/system/fonts/NotoSans-Regular.ttf")) fonts.add("/system/fonts/NotoSans-Regular.ttf");
	else if (platform_file_exists("/system/fonts/Roboto-Regular.ttf"  )) fonts.add("/system/fonts/Roboto-Regular.ttf");
	else if (platform_file_exists("/system/fonts/DroidSans.ttf"       )) fonts.add("/system/fonts/DroidSans.ttf");

	if      (file_japanese != nullptr)                                      fonts.add(file_japanese);
	else if (platform_file_exists("/system/fonts/NotoSansCJK-Regular.ttc")) fonts.add("/system/fonts/NotoSansCJK-Regular.ttc");
	else if (platform_file_exists("/system/fonts/DroidSansJapanese.ttf"  )) fonts.add("/system/fonts/DroidSansJapanese.ttf");

	if (fonts.count > 0)
		result = font_create_files(fonts.data, fonts.count);

#if __ANDROID_API__ >= 29
	if (font_latin   ) AFont_close(font_latin);
	if (font_japanese) AFont_close(font_japanese);
	AFontMatcher_destroy(matcher);
#endif
	fonts.free();
	return result;
}

///////////////////////////////////////////

void platform_iterate_dir(const char* directory_path, void* callback_data, void (*on_item)(void* callback_data, const char* name, bool file)) {}

///////////////////////////////////////////

#include <unwind.h>
#include <dlfcn.h>
#include <cxxabi.h>

struct android_backtrace_state {
	void **current;
	void **end;
};

void platform_print_callstack() {
	const int max = 100;
	void* buffer[max];

	android_backtrace_state state;
	state.current = buffer;
	state.end = buffer + max;

	_Unwind_Backtrace([](struct _Unwind_Context* context, void* arg) {
		android_backtrace_state* state = (android_backtrace_state *)arg;
		uintptr_t pc = _Unwind_GetIP(context);
		if (pc) {
			if (state->current == state->end)
				return (_Unwind_Reason_Code)_URC_END_OF_STACK;
			else
				*state->current++ = reinterpret_cast<void*>(pc);
		}
		return (_Unwind_Reason_Code)_URC_NO_REASON;
	}, &state);

	int count = (int)(state.current - buffer);

	for (int idx = 0; idx < count; idx++)  {
		const void* addr   = buffer[idx];
		const char* symbol = "";

		Dl_info info;
		if (dladdr(addr, &info) && info.dli_sname)
			symbol = info.dli_sname;
		int   status    = 0; 
		char *demangled = __cxxabiv1::__cxa_demangle(symbol, 0, 0, &status); 

		sk::log_diagf("%03d: 0x%p %s", idx, addr,
			(nullptr != demangled && 0 == status) ?
			demangled : symbol);

		sk_free(demangled);
	}
}

///////////////////////////////////////////

void android_log_channel_open() {
	if (android_log_open) return;
	android_log_open = true;
	const char* tag = log_get_tag();
	if (strcmp(tag, "") == 0)
		tag = "StereoKit";
	openlog(tag, LOG_CONS | LOG_NOWAIT, LOG_USER);
}

///////////////////////////////////////////

void android_log_channel_close() {
	if (!android_log_open) return;
	android_log_open = false;
	closelog();
}

///////////////////////////////////////////

void platform_debug_output(log_ level, const char *text) {
	android_log_channel_open();

	int32_t priority = LOG_INFO;
	if      (level == log_diagnostic) priority = LOG_DEBUG;
	else if (level == log_inform    ) priority = LOG_INFO;
	else if (level == log_warning   ) priority = LOG_WARNING;
	else if (level == log_error     ) priority = LOG_ERR;
	syslog(priority, "%s", text);
}

///////////////////////////////////////////

void platform_sleep(int ms) {
	usleep(ms * 1000);
}

} // namespace sk

#endif