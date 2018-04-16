
-- functions call by csharp
-- game logic
------------------------------------------------------------------------------------------
local network
local sharplua = _G.sharplua
function init()
end

function start()
	-- local test = require "test"
	-- test()
	local fw = require "ejoy2d.framework"

	local package = require "ej_package"
	-- package:load_coco("ui2", "res/asset/ui2")

	-- package.load_coco("scene_bg1", "res/scene_bg1")
	-- package:load_coco("characters2", "res/characters2")
	print("lua start")
end

function update(deltaTime, manCount)
end

function fixedupdate(fixedDeltaTime)
end

function request( ... )
end

function close( ... )
	print("lua close")
end

-- functions call by csharp
-- misc
------------------------------------------------------------------------------------------
function set_print(print_sharp)
	print = function( ... )
		sharplua.call(print_sharp, ...)
	end
end

function set_call_sharp(message_sharp)
	call_sharp = function(type, ... )
		local ok, err = pcall(sharplua.call, message_sharp, type, ...)
	end
end

function unity_res_load(loader)
	local function do_load(name, script)
		local obj, err = load(script, name)
		if not obj then
			error("load script failed:"..name.."\n"..err)
		end
		return obj()
	end

	package.searchers[2] = function(name)
		name = string.gsub(name, "%.", "/")
		local script = sharplua.call(loader, "lua/"..name)
		return do_load, script
	end

	resource_load = function(path)
		return sharplua.call(loader, path)
	end
end

--------------------------------------------------------------------
function load_package( ... )
	local package = require "ej_package"
	package:load_coco(...)
end

function save_package(...)
	local package = require "ej_package"
	package:save_package(...)
end

function call_by_sharp(input_type, ...)
	_G[input_type](...)
end
