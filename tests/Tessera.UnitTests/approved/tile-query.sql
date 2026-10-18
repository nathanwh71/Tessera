SELECT ST_AsMVT(tile.*, @layer, @resolution, 'tessera_shape') FROM (
  SELECT ST_AsMVTGeom(ST_Transform(t."geom", 3857), ST_TileEnvelope(@z, @x, @y),
                      @resolution, @buffer, true) AS tessera_shape, "id", "layer", "group", "name"
  FROM "food"."cafes" AS t
  WHERE t."geom" && ST_Transform(ST_TileEnvelope(@z, @x, @y, margin => @margin), @srid)
  ORDER BY t."fid"
  LIMIT @limit
) AS tile
