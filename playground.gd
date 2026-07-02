extends Node2D

@onready var tilemap: TileMapLayer = $Map/TileMapLayer
@onready var camera: Camera2D = $Camera2D

func _ready() -> void:
	# Calculate bounds based on used tiles
	var map_rect = tilemap.get_used_rect()
	var tile_size = tilemap.tile_set.tile_size
	
	# Apply pixel boundaries to the camera limits
	camera.limit_left = map_rect.position.x * tile_size.x
	camera.limit_right = map_rect.end.x * tile_size.x
	camera.limit_top = map_rect.position.y * tile_size.y
	camera.limit_bottom = map_rect.end.y * tile_size.y
