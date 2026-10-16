-- Made by 'npm run etl' from p.json. DbUp runs this script once (ADR 0009).
-- Never change its SQL after it has run: DbUp would not run it again. Write a new script instead.
CREATE SCHEMA IF NOT EXISTS "p";
SET standard_conforming_strings = ON;
DROP TABLE IF EXISTS "p"."all" CASCADE;
CREATE TABLE "p"."all"();
ALTER TABLE "p"."all" ADD COLUMN "fid" SERIAL CONSTRAINT "all_pk" PRIMARY KEY;
SELECT AddGeometryColumn('p','all','geom',4326,'GEOMETRY',2);
ALTER TABLE "p"."all" ADD COLUMN "id" VARCHAR;
ALTER TABLE "p"."all" ADD COLUMN "name" VARCHAR;
ALTER TABLE "p"."all" ADD COLUMN "n" INTEGER;
ALTER TABLE "p"."all" ADD COLUMN "mixed" VARCHAR;
ALTER TABLE "p"."all" ADD COLUMN "real" FLOAT8;
ALTER TABLE "p"."all" ADD COLUMN "tags" VARCHAR;
ALTER TABLE "p"."all" ADD COLUMN "obj" VARCHAR;
ALTER TABLE "p"."all" ADD COLUMN "flag" BOOLEAN;
ALTER TABLE "p"."all" ADD COLUMN "big" INT8;
ALTER TABLE "p"."all" ADD COLUMN "layer" VARCHAR;
ALTER TABLE "p"."all" ADD COLUMN "words" VARCHAR;
ALTER TABLE "p"."all" ADD COLUMN "late" VARCHAR;
ALTER TABLE "p"."all" ADD COLUMN "reals" VARCHAR;
INSERT INTO "p"."all" ("geom", "id", "name", "n", "mixed", "real", "tags", "obj", "flag", "big", "layer", "words", "late", "reals") VALUES ('0101000020E61000003333333333635C40713D0AD7A3303640', 'a-1', 'One', 1, '1', 0.3, '[ 1, 2 ]', '{ "k": [ 1.5, "x" ] }', 't', 9007199254740993, 'a', NULL, NULL, NULL);
INSERT INTO "p"."all" ("geom", "id", "name", "n", "mixed", "real", "tags", "obj", "flag", "big", "layer", "words", "late", "reals") VALUES ('0101000020E61000002506819543635C4037894160E5303640', 'a-2', 'Two', NULL, 'two', 3.0, NULL, NULL, 'f', NULL, 'a', '[ "x", "y" ]', NULL, NULL);
INSERT INTO "p"."all" ("id", "name", "n", "mixed", "real", "tags", "obj", "flag", "big", "layer", "words", "late", "reals") VALUES (NULL, NULL, 3, NULL, 1e-07, NULL, 'plain', NULL, NULL, 'a', NULL, 'only here', NULL);
INSERT INTO "p"."all" ("geom", "id", "name", "n", "mixed", "real", "tags", "obj", "flag", "big", "layer", "words", "late", "reals") VALUES ('0102000020E6100000020000003333333333635C40713D0AD7A3303640A4703D0AD7635C403333333333333640', 'other', 'Four', NULL, NULL, 12.5, NULL, NULL, NULL, NULL, 'a', NULL, NULL, '[ 1.25, 0.1 ]');
INSERT INTO "p"."all" ("geom", "id", "name", "n", "mixed", "real", "tags", "obj", "flag", "big", "layer", "words", "late", "reals") VALUES ('0106000020E610000002000000010300000001000000040000007B14AE47E1625C4090C2F5285C2F36403333333333635C40713D0AD7A33036403333333333635C40AE47E17A142E36407B14AE47E1625C4090C2F5285C2F364001030000000100000004000000C3F5285C8F625C40AE47E17A142E3640C3F5285C8F625C40713D0AD7A33036407B14AE47E1625C4090C2F5285C2F3640C3F5285C8F625C40AE47E17A142E3640', 'a-5', 'Five', 5, NULL, NULL, NULL, NULL, NULL, NULL, 'b', NULL, NULL, NULL);
CREATE INDEX "all_geom_geom_idx" ON "p"."all" USING GIST ("geom");
ANALYZE "p"."all";
