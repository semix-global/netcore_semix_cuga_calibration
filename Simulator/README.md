# 1. install tool

---

```bash
# install uv
$ powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"

# install dvc
uv tool install "dvc[ssh]"
```

# 2. init

---

```bash
# 打开到项目目录
$ cd .\netcore_semix_cuga_calibration
$ dvc init
$ dvc remote add -d cuga_calibration ssh://root@192.168.60.201/data1/dvc_repo_cuga_calibration
$ dvc config core.autostage true
$ git add ./
$ git commit -m"init dvc"
$ git push
```

# 3. usage

---

>   -   了解
>
>   ```bash
>   # 单个文件: 添加大文件(不允许, 了解下单个文件命令)
>   $ dvc add data/raw.bin
>   ```

```bash
# 1. 打开到项目目录
$ cd .\netcore_semix_cuga_calibration

# 2. 添加文件夹, 主要使用这个(必须使用这个)
$ dvc add .\Simulator\B3\

# 3. 上传数据
$ dvc push

# 4. 常规git追踪
$ git add ./
$ git commit -m"***"
$ git push

# 5. 拉取所有
$ dvc pull

# 5. 拉取单个(通过dvc文件获取, 指定目录)
$ dvc pull .\Simulator\B3.dvc
$ dvc pull .\Simulator\B3\Common\Windows\Tools\Stage\RTAWindowViewModel\

# 查看上传状态
$ dvc status -c

# 查看所有 DVC 管理的文件和目录
$ dvc list . --dvc-only --tree
.
└── Simulator
    └── B3
        └── Common
            └── Windows
                └── Tools
                    └── Stage
                        └── RTAWindowViewModel
                            ├── 20261029_13_0_0_1_short_1873008_PMT08-CH3_8.raw
                            └── 20261029_17_0_0_1_short_1873008_PMT08-CH3_8.raw
$ dvc list . --dvc-only --recursive

# 查看某一个文件夹 DVC 管理的文件和目录
$ dvc list . .\Simulator\B3\ --dvc-only --tree
./Simulator/B3/
└── Common
    └── Windows
        └── Tools
            └── Stage
                └── RTAWindowViewModel
                    ├── 20261029_13_0_0_1_short_1873008_PMT08-CH3_8.raw
                    └── 20261029_17_0_0_1_short_1873008_PMT08-CH3_8.raw
$ dvc list . .\Simulator\B3\ --dvc-only --recursive
```

