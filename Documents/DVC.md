# 1. install tool

---

```bash
# install uv
$ powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"

# install dvc
uv tool install dvc
```

>   1.   安装putty以及winscp
>   2.   配置putty和winscp ssh连接

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

```bash
# 添加大文件
$ dvc add data/raw.bin

# 上传数据
$ dvc push

# 查看上传状态
$ dvc status -c
```

