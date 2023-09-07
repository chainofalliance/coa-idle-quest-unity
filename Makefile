#!make


include .env
export

.PHONY: build
build:	
	scripts/main.sh

