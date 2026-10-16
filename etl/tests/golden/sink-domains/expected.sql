-- Made by 'npm run etl' from p.json. DbUp runs this script once (ADR 0009).
-- Never change its SQL after it has run: DbUp would not run it again. Write a new script instead.
CREATE SCHEMA IF NOT EXISTS "sink";
SET standard_conforming_strings = ON;
DROP TABLE IF EXISTS "sink"."labels" CASCADE;
CREATE TABLE "sink"."labels"();
ALTER TABLE "sink"."labels" ADD COLUMN "fid" SERIAL CONSTRAINT "labels_pk" PRIMARY KEY;
SELECT AddGeometryColumn('sink','labels','geom',4326,'POINT',2);
ALTER TABLE "sink"."labels" ADD COLUMN "size" VARCHAR;
ALTER TABLE "sink"."labels" ADD COLUMN "size_resolved" VARCHAR;
ALTER TABLE "sink"."labels" ADD COLUMN "kind" INTEGER;
ALTER TABLE "sink"."labels" ADD COLUMN "kind_resolved" VARCHAR;
INSERT INTO "sink"."labels" ("geom", "size", "size_resolved", "kind", "kind_resolved") VALUES ('0101000020E61000003333333333635C40713D0AD7A3303640', 'S', 'Small', 2, 'Library');
INSERT INTO "sink"."labels" ("geom", "size", "size_resolved", "kind", "kind_resolved") VALUES ('0101000020E6100000A4703D0AD7635C403333333333333640', 'X', NULL, NULL, NULL);
CREATE INDEX "labels_geom_geom_idx" ON "sink"."labels" USING GIST ("geom");
ANALYZE "sink"."labels";
