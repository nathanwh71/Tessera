-- Made by 'npm run etl' from p.json. DbUp runs this script once (ADR 0009).
-- Never change its SQL after it has run: DbUp would not run it again. Write a new script instead.
CREATE SCHEMA IF NOT EXISTS "culture";
SET standard_conforming_strings = ON;
DROP TABLE IF EXISTS "culture"."places" CASCADE;
CREATE TABLE "culture"."places"();
ALTER TABLE "culture"."places" ADD COLUMN "fid" SERIAL CONSTRAINT "places_pk" PRIMARY KEY;
SELECT AddGeometryColumn('culture','places','geom',4326,'POINT',2);
ALTER TABLE "culture"."places" ADD COLUMN "name" VARCHAR;
ALTER TABLE "culture"."places" ADD COLUMN "opened" timestamp with time zone;
ALTER TABLE "culture"."places" ADD COLUMN "visited" timestamp with time zone;
ALTER TABLE "culture"."places" ADD COLUMN "height" FLOAT8;
INSERT INTO "culture"."places" ("geom", "name", "opened", "visited", "height") VALUES ('0101000020E61000003333333333635C40713D0AD7A3303640', 'A', '2024-05-01T00:00:00Z', '2024-05-01T10:30:00Z', 12.5);
INSERT INTO "culture"."places" ("geom", "name", "opened", "visited", "height") VALUES ('0101000020E6100000A4703D0AD7635C40AE47E17A142E3640', 'B', '2023-01-02T00:00:00Z', '2023-01-02T08:00:00Z', 3.0);
CREATE INDEX "places_geom_geom_idx" ON "culture"."places" USING GIST ("geom");
ANALYZE "culture"."places";
SET standard_conforming_strings = ON;
DROP TABLE IF EXISTS "culture"."codes" CASCADE;
CREATE TABLE "culture"."codes"();
ALTER TABLE "culture"."codes" ADD COLUMN "fid" SERIAL CONSTRAINT "codes_pk" PRIMARY KEY;
SELECT AddGeometryColumn('culture','codes','geom',4326,'GEOMETRY',2);
ALTER TABLE "culture"."codes" ADD COLUMN "code" VARCHAR;
ALTER TABLE "culture"."codes" ADD COLUMN "label" VARCHAR;
INSERT INTO "culture"."codes" ("code", "label") VALUES ('1', 'Museum');
INSERT INTO "culture"."codes" ("code", "label") VALUES ('2', 'Library');
CREATE INDEX "codes_geom_geom_idx" ON "culture"."codes" USING GIST ("geom");
ANALYZE "culture"."codes";
