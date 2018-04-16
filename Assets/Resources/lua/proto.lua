
local sproto = require "sproto"

local proto = {}


local sproto_schema_src = [[
.type {
    .field {
        name 0 : string
        buildin 1 : integer
        type 2 : integer
        tag 3 : integer
        array 4 : boolean
        key 5 : integer # If key exists, array must be true, and it's a map.
    }
    name 0 : string
    fields 1 : *field
}

.protocol {
    name 0 : string
    tag 1 : integer
    request 2 : integer # index
    response 3 : integer # index
}

.group {
    type 0 : *type
    protocol 1 : *protocol
}
]]


local P, T
local function loadproto()
	local txt = assert(resource_load("proto/lobby.sproto"))

	local sprotoparser = require "sprotoparser"
	local bin = sprotoparser.parse(txt)

	P = sproto.new(bin)

	local sproto_schema = sproto.parse(sproto_schema_src)
	local schema = sproto_schema:decode("group", bin)

	local type_fields = {}
	for k, v in ipairs(schema.type) do
		type_fields[v.name] = {default=proto.default(v.name)}
		local fields = type_fields[v.name]
		for m, n in ipairs(v.fields) do
			table.insert(fields, n.name)
		end
	end
	T = type_fields
end

function proto.to_params(ptype, data)
	local fields = rawget(T, ptype)
	local params = {}
	for _, v in ipairs(fields) do
		local val = rawget(data, v)
		if not val then
			val = rawget(fields.default, v)
		end
		if type(val) == "table" then
			local json = require "json"
			val = json:encode({Items=val})
		end
		table.insert(params, val)
	end
	return params
end

function proto.request(type, obj)
	local data , tag = P:request_encode(type,obj)
	return sproto.pack(string.pack("<I4",tag) .. data), tag
end

function proto.response(type, blob)
	local data = sproto.unpack(blob)
	return P:response_decode(type, data)
end

function proto.decode_type(...)
	return P:pdecode(...)
end

function proto.encode_type(...)
	return P:pencode(...)
end

function proto.default(...)
	local ret = P:default(...)
	for k, v in pairs(ret) do
		if type(v) == "table" and v.__type then
			ret[k] = proto.default(v.__type)
		end
	end
	return ret
end


loadproto()

-- for k, v in pairs(T) do
-- 	print(k..":"..table.concat(v, ";"))
-- end

-- local x = proto.to_params("package", {index=3,xx=4})
-- print("...........params:")
-- print(table.unpack(x))


return proto
