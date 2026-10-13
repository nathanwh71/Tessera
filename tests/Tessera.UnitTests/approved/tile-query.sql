SELECT ST_AsMVT(tile.*, @p0, @p1, 'tessera_shape') FROM (
  SELECT ST_AsMVTGeom(ST_Transform(t."geom", 3857), ST_TileEnvelope(@p2, @p3, @p4), @p5, @p6, true) AS tessera_shape, "group", "layer", "name"
  FROM "food"."cafes" AS t
  WHERE t."geom" && ST_Transform(ST_TileEnvelope(@p7, @p8, @p9, margin => @p10), @p11)
  LIMIT @p12
) AS tile
