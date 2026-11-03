# Makefile cho MASTER_KEY: terminal UI va CLI mkv
# Dung GNU make (da co san qua WinGet ezwinports). Chi dung lenh dotnet nen chay
# duoc ca cmd / PowerShell / sh.
#   make help                 - hien thi huong dan
#   make build                - build Release ca 2 project
#   make run                  - mo TUI mkv
#   make run-cli KEY=<TEN>    - chay mkv tra cuu 1 key, vd: make run-cli KEY=CONTROL_PLANE_TUNNEL_ID
#   make selftest             - roundtrip ma hoa/giai ma + NormalizeKey (khong dung DPAPI that)
#   make publish              - publish single-file ca 2 project + dong goi installer (dist/*.exe)
#   make install              - publish mkv vao %USERPROFILE%\.tools
#   make installer            - dong goi installer tu publish/ (can Inno Setup 6)
#   make clean                - clean build output
#   make del                  - xoa DPAPI (Credential MyPersonalVault_MasterKey) de gia lap chua co key

DOTNET  ?= dotnet
CONFIG  ?= Release
RID     ?= win-x64
APP_VERSION ?= 1.0.0


CLI := mkv/mkv.csproj
ISS := installer/MasterKeyVault.iss
TOOLS_DIR := $(USERPROFILE)/.tools

.DEFAULT_GOAL := help

.PHONY: help build run run-cli selftest publish publish-cli publish-app installer install clean del

help:
	@echo "Targets:"
	@echo "  make build                - build mkv TUI/CLI"
	@echo "  make run                  - mo giao dien TUI mkv"
	@echo "  make run-cli KEY=<TEN>    - mkv tra cuu 1 key (vd KEY=CONTROL_PLANE_TUNNEL_ID)"
	@echo "  mkv --import-browser-csv <file.csv> - nhap password trinh duyet vao vault"
	@echo "  mkv --export-browser-csv <file.csv> - xuat password trinh duyet ra CSV"
	@echo "  make selftest             - mkv --selftest (khong dung DPAPI/vault that)"
	@echo "  make publish              - publish + dong goi installer dist/MasterKeyVault-Setup-<ver>.exe"
	@echo "  make publish-cli          - publish mkv -> mkv/publish/mkv.exe"
	@echo "  make publish-app          - alias cua publish-cli"
	@echo "  make installer            - dong goi installer tu publish/ (can Inno Setup 6)"
	@echo "  make install              - publish mkv vao %USERPROFILE%\.tools"
	@echo "  make clean                - clean build output"
	@echo "  make del                  - xoa DPAPI de gia lap chua co key"

build:
	$(DOTNET) build $(CLI) -c $(CONFIG)

run:
	$(DOTNET) run --project $(CLI) -c $(CONFIG) -- --tui

run-cli:
	$(DOTNET) run --project $(CLI) -c $(CONFIG) -- $(KEY)

selftest:
	$(DOTNET) run --project $(CLI) -c $(CONFIG) -- --selftest

publish: installer

publish-cli:
	if exist mkv\publish rmdir /s /q mkv\publish
	$(DOTNET) publish $(CLI) -c $(CONFIG) -r $(RID) --self-contained false -p:PublishSingleFile=true -o mkv/publish

publish-app: publish-cli

installer: publish-cli
	powershell -NoProfile -ExecutionPolicy Bypass -File installer/build-installer.ps1 -Version "$(APP_VERSION)"

install:
	$(DOTNET) publish $(CLI) -c $(CONFIG) -r $(RID) --self-contained false -p:PublishSingleFile=true -o "$(TOOLS_DIR)"
	@echo "Installed mkv.exe to $(TOOLS_DIR). Mo terminal moi de nhan PATH."
clean:
	$(DOTNET) clean $(CLI) -c $(CONFIG)

del:
	-cmdkey /delete:MyPersonalVault_MasterKey
	@echo "Da xoa DPAPI MyPersonalVault_MasterKey (neu co). App se roi vao view Khoi phuc."
