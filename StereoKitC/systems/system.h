#pragma once

#include <stdint.h>

namespace sk {

struct system_t {
	const char  *name;
	const char **init_dependencies;
	int32_t      init_dependency_count;
	const char **step_dependencies;
	int32_t      step_dependency_count;

	uint64_t profile_frame_start;
	uint64_t profile_frame_duration;

	uint64_t profile_step_count;
	uint64_t profile_step_duration;
	uint64_t profile_start_duration;
	uint64_t profile_shutdown_duration;

	bool (*func_initialize)(void);
	void (*func_step      )(void);
	void (*func_shutdown  )(void);
};

void systems_add       (const system_t *system);
bool systems_initialize();
void systems_step      ();
void systems_shutdown  ();
system_t *systems_find (const char *name);

} // namespace sk