@echo off
if not exist .venv (
    py -m venv .venv
)
call .venv\Scripts\activate.bat
.venv\Scripts\python -m pip install -r requirements.txt --quiet
func start --port 7072