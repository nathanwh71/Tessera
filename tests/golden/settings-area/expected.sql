-- Made by 'npm run etl' from p.json. DbUp runs this script once (ADR 0009).
-- Never change its SQL after it has run: DbUp would not run it again. Write a new script instead.
CREATE SCHEMA IF NOT EXISTS "a";
SET standard_conforming_strings = ON;
DROP TABLE IF EXISTS "a"."b" CASCADE;
CREATE TABLE "a"."b"();
ALTER TABLE "a"."b" ADD COLUMN "fid" SERIAL CONSTRAINT "b_pk" PRIMARY KEY;
SELECT AddGeometryColumn('a','b','geom',4326,'POINT',2);
ALTER TABLE "a"."b" ADD COLUMN "name" VARCHAR;
ALTER TABLE "a"."b" ADD COLUMN "opened" timestamp with time zone;
ALTER TABLE "a"."b" ADD COLUMN "visited" timestamp with time zone;
ALTER TABLE "a"."b" ADD COLUMN "height" FLOAT8;
INSERT INTO "a"."b" ("geom", "name", "opened", "visited", "height") VALUES ('0101000020E61000003333333333635C40713D0AD7A3303640', 'A', '2024-05-01T00:00:00Z', '2024-05-01T10:30:00Z', 12.5);
INSERT INTO "a"."b" ("geom", "name", "opened", "visited", "height") VALUES ('0101000020E6100000A4703D0AD7635C40AE47E17A142E3640', 'B', '2023-01-02T00:00:00Z', '2023-01-02T08:00:00Z', 3);
CREATE INDEX "b_geom_geom_idx" ON "a"."b" USING GIST ("geom");
ANALYZE "a"."b";
