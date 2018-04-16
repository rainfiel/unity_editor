
local spritepack = require "ejoy2d.spritepack"
local pack_c = require "ejoy2d.spritepack.c"
local json = require "json"

local M = {packages = {}}

local function print_tbl(tbl, ref)
	if not tbl then return "" end
    ref = ref or {} --{depth=3}
    if ref.depth and ref.depth < 0 then return "..." end
    local dbg = {}
    tbl = tbl or {}
    for k, v in pairs(tbl) do
        if type(v) == "table" then
            if ref[v] then
                table.insert(dbg, tostring(k).."=".."ref_"..tostring(v))
            else
                ref[v] = true
                if ref.depth then
	    			ref.depth = ref.depth - 1
	    		end
                table.insert(dbg, tostring(k).."="..print_tbl(v, ref).."\n")
                if ref.depth then
	    			ref.depth = ref.depth + 1
	    		end
            end
        else
            table.insert(dbg, tostring(k).."="..tostring(v))
        end
    end
    return "{"..table.concat(dbg, ",").."}"
end

local function pack_bgra(color)
	local c = 	((color&0x000000FF) << 24 ) | 
				((color&0x0000ff00) << 8) | 
				((color&0x00ff0000) >> 8) |
				((color&0xff000000) >> 24)
	return pack_c.color(c)
end

function M:save_package(packname, filename, data)
	local tbl = {}

	local pictures = self.packages[packname].pictures
	for _, p in ipairs(pictures) do
		table.insert(tbl, p)
	end

	local anis = json:decode(data)
	for _, v in ipairs(anis.Items) do
		if v.export == "" then v.export = nil end

		for _, a in ipairs(v.actions) do
			local act = {}
			for m, n in ipairs(a.frames) do
				table.insert(act, n.parts)
			end
			table.insert(v, act)
		end
		v.actions = nil
		table.insert(tbl, v)
		-- print(print_tbl(v))	
	end

	local meta = assert(spritepack.pack(tbl))
	local bin = spritepack.export(meta)

	print("save_package:"..filename)

	local file = io.open(filename..".bytes", "w+b")
	file:write(bin)
	file:close()
end

function M:load_coco(packname, filename)
	local data = resource_load(filename..".coco")

	local p = {}
	local tbl = {}
	local tex_cnt

	local env = {}
	env.texture = function(cnt)
		tex_cnt = cnt
	end

	if not self.packages[packname] then
		self.packages[packname] = {}
	end
	local obj = self.packages[packname]
	obj.pictures = {}
	call_sharp("new_package_start", packname, filename)

	local types = {[[picture]], [[animation]], [[polygon]], [[label]], [[pannel]]}
	for k, v in ipairs(types) do
		rawset(env, v, function(data)
			data.type = v
			table.insert(tbl, data)
			if v == "picture" or v == "polygon" then
				table.insert(obj.pictures, data)

				local packs = {}
				for i, p in ipairs(data) do
					if p.tex then
						p.tex = p.tex + 1
						p.tex_name = filename..p.tex
					end
					table.insert(packs, p)
				end
				local pack = json:encode({Items=packs})
				call_sharp("new_picture", data.id, pack)
			elseif v == "animation" then
				local st = {id=data.id, export=data.export, type=v, component=data.component, actions={}}
				for _, act in ipairs(data) do
					local action = {frames={}}
					for m, n in ipairs(act) do
						local frames = {parts={}}
						for x, y in ipairs(n) do
							table.insert(frames.parts, y)
						end
						table.insert(action.frames, frames)
					end
					table.insert(st.actions, action)
				end
				local pack = json:encode(st)
				call_sharp("new_animation", pack)
			end
		end)
	end

	load(data, packname, "t", env)()

	spritepack.pack_color = pack_bgra
	p.meta = assert(spritepack.pack(tbl))
	spritepack.pack_color = pack_c.color

	p.tex = {}
	for i=1,p.meta.texture do
		local path = filename..i..".png"
		-- local tex_id = texture:query_texture(path)
		-- if not tex_id then
		-- 	tex_id = texture:add_texture(path)
		-- 	ppm.texture(tex_id, path)
		-- end
		p.tex[i] = 1
	end
	-- spritepack.init(packname, p.tex, p.meta)
	
	call_sharp("new_package_end")

	return p
end


return M