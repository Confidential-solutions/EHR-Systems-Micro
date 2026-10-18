.PHONY: up down logs env frontend

env:
	cp -n .env.example .env.development || true
	@echo "Edit .env.development with strong secrets before starting."

up: env
	docker compose --env-file .env.development up -d

down:
	docker compose --env-file .env.development down

logs:
	docker compose --env-file .env.development logs -f

frontend:
	cd frontend && npm install && npm start
