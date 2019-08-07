#pragma once

#include <d3d11.h>

#include "stereokit.h"
#include "assets.h"

enum shaderarg_type_ {
	shaderarg_type_float = 0,
	shaderarg_type_color,
	shaderarg_type_vector,
	shaderarg_type_matrix,
};
extern const size_t shaderarg_size[];

struct shaderargs_data_t {
	void    *buffer;
	tex2d_t *textures;
};

struct shaderargs_t {
	ID3D11Buffer *const_buffer;
	int buffer_slot;
	int buffer_size;
};

struct shaderargs_desc_item_t {
	uint64_t        id;
	size_t          offset;
	size_t          size;
	shaderarg_type_ type;
};

struct shaderargs_desc_t {
	shaderargs_desc_item_t *item;
	int item_count;
	int buffer_size;
};

struct shader_tex_slots_item_t {
	uint64_t id;
	int      slot;
};

struct shader_tex_slots_t {
	shader_tex_slots_item_t *tex;
	int tex_count;
};

struct _shader_t {
	asset_header_t      header;
	ID3D11VertexShader *vshader;
	ID3D11PixelShader  *pshader;
	ID3D11InputLayout  *vert_layout;
	shaderargs_t        args;
	shaderargs_desc_t   args_desc;
	shader_tex_slots_t  tex_slots;
};

void shader_destroy(shader_t shader);

void shaderargs_create    (shaderargs_t &args, size_t buffer_size, int buffer_slot);
void shaderargs_destroy   (shaderargs_t &args);
void shaderargs_set_data  (shaderargs_t &args, void *data);
void shaderargs_set_active(shaderargs_t &args);