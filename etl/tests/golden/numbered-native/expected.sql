-- Made by 'npm run etl' from p.json. DbUp runs this script once (ADR 0009).
-- Never change its SQL after it has run: DbUp would not run it again. Write a new script instead.
CREATE SCHEMA IF NOT EXISTS "p";
SET standard_conforming_strings = ON;
DROP TABLE IF EXISTS "p"."numbered" CASCADE;
CREATE TABLE "p"."numbered"();
ALTER TABLE "p"."numbered" ADD COLUMN "fid" SERIAL CONSTRAINT "numbered_pk" PRIMARY KEY;
SELECT AddGeometryColumn('p','numbered','geom',4326,'GEOMETRY',2);
ALTER TABLE "p"."numbered" ADD COLUMN "id" VARCHAR;
ALTER TABLE "p"."numbered" ADD COLUMN "name" VARCHAR;
ALTER TABLE "p"."numbered" ADD COLUMN "n" INTEGER;
ALTER TABLE "p"."numbered" ADD COLUMN "mixed" VARCHAR;
ALTER TABLE "p"."numbered" ADD COLUMN "real" FLOAT8;
ALTER TABLE "p"."numbered" ADD COLUMN "tags" VARCHAR;
ALTER TABLE "p"."numbered" ADD COLUMN "obj" VARCHAR;
ALTER TABLE "p"."numbered" ADD COLUMN "flag" BOOLEAN;
ALTER TABLE "p"."numbered" ADD COLUMN "big" INT8;
ALTER TABLE "p"."numbered" ADD COLUMN "layer" VARCHAR;
ALTER TABLE "p"."numbered" ADD COLUMN "words" VARCHAR;
ALTER TABLE "p"."numbered" ADD COLUMN "late" VARCHAR;
ALTER TABLE "p"."numbered" ADD COLUMN "reals" VARCHAR;
INSERT INTO "p"."numbered" ("geom", "id", "name", "n", "mixed", "real", "tags", "obj", "flag", "big", "layer", "words", "late", "reals") VALUES ('0101000020E61000003333333333635C40713D0AD7A3303640', '10', 'One', 1, '1', 0.3, '[ 1, 2 ]', '{ "k": [ 1.5, "x" ] }', 't', 9007199254740993, 'a', NULL, NULL, NULL);
INSERT INTO "p"."numbered" ("geom", "id", "name", "n", "mixed", "real", "tags", "obj", "flag", "big", "layer", "words", "late", "reals") VALUES ('0101000020E61000002506819543635C4037894160E5303640', 'a-2', 'Two', NULL, 'two', 3, NULL, NULL, 'f', NULL, 'a', '[ "x", "y" ]', NULL, NULL);
INSERT INTO "p"."numbered" ("id", "name", "n", "mixed", "real", "tags", "obj", "flag", "big", "layer", "words", "late", "reals") VALUES ('30', NULL, 3, NULL, 1E-07, NULL, 'plain', NULL, NULL, 'a', NULL, 'only here', NULL);
INSERT INTO "p"."numbered" ("geom", "id", "name", "n", "mixed", "real", "tags", "obj", "flag", "big", "layer", "words", "late", "reals") VALUES ('0102000020E6100000020000003333333333635C40713D0AD7A3303640A4703D0AD7635C403333333333333640', 'other', 'Four', NULL, NULL, 12.5, NULL, NULL, NULL, NULL, 'a', NULL, NULL, '[ 1.25, 0.1 ]');
INSERT INTO "p"."numbered" ("geom", "id", "name", "n", "mixed", "real", "tags", "obj", "flag", "big", "layer", "words", "late", "reals") VALUES ('0103000020E61000000100000005000000C3F5285C8F625C40AE47E17A142E36403333333333635C40713D0AD7A33036403333333333635C40AE47E17A142E3640C3F5285C8F625C40713D0AD7A3303640C3F5285C8F625C40AE47E17A142E3640', '50', 'Five', 5, NULL, NULL, NULL, NULL, NULL, NULL, 'b', NULL, NULL, NULL);
CREATE INDEX "numbered_geom_geom_idx" ON "p"."numbered" USING GIST ("geom");
ANALYZE "p"."numbered";
