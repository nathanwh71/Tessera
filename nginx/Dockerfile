# The Nginx that stands in front of the service (ADR 0008). npm start and the browser tests build
# this image, so the tests check the Nginx the office runs. The configuration stays outside the
# image, in default.conf.template, which each container mounts, so a change to it needs no build.
# The tag holds Nginx to 1.29, so only its patch releases arrive without a change to this line;
# Alpine keeps the image small. Dependabot proposes each newer tag (.github/dependabot.yml).
FROM nginx:1.29-alpine
