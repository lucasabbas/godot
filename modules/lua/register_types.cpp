#include "register_types.h"
#include "core/class_db.h"
#include "lua.h"

void register_lua_types(){
	ClassDB::register_class<Lua>();
}

void unregister_lua_types() {
}