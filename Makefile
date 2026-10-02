# AdofaiPro - 构建 / 部署
#
# 常用：
#   make            # Debug 构建
#   make release    # Release 构建
#   make deploy     # 构建并复制到游戏 Mods/AdofaiPro
#   make package    # 生成 dist/AdofaiPro/ 可安装目录
#   make clean
#
# 可覆盖变量：
#   make MANAGED_DIR=/path/to/Managed MODS_DIR=/path/to/Mods

SHELL        := /bin/bash
MOD_NAME     := AdofaiPro
CONFIG       ?= Debug
DOTNET       ?= dotnet
MANAGED_DIR  ?= /home/argvchs/workspace/Managed
MODS_DIR     ?= $(HOME)/.steam/steam/steamapps/common/A Dance of Fire and Ice/Mods
OUT          := bin/$(CONFIG)
DIST         := dist/$(MOD_NAME)

.PHONY: all build release clean deploy package

all: build

build:
	$(DOTNET) build $(MOD_NAME).csproj -c $(CONFIG) -p:ManagedDir="$(MANAGED_DIR)"

release:
	$(DOTNET) build $(MOD_NAME).csproj -c Release -p:ManagedDir="$(MANAGED_DIR)"

clean:
	rm -rf bin obj dist

deploy: build
	mkdir -p "$(MODS_DIR)/$(MOD_NAME)"
	cp -f "$(OUT)/$(MOD_NAME).dll" "$(MODS_DIR)/$(MOD_NAME)/"
	-if [ -f "$(OUT)/$(MOD_NAME).pdb" ]; then cp -f "$(OUT)/$(MOD_NAME).pdb" "$(MODS_DIR)/$(MOD_NAME)/"; fi
	cp -f Info.json "$(MODS_DIR)/$(MOD_NAME)/"
	@echo "Deployed to $(MODS_DIR)/$(MOD_NAME)"

package: release
	rm -rf "$(DIST)"
	mkdir -p "$(DIST)"
	cp -f "bin/Release/$(MOD_NAME).dll" "$(DIST)/"
	cp -f Info.json "$(DIST)/"
	@echo "Package ready in $(DIST)"
