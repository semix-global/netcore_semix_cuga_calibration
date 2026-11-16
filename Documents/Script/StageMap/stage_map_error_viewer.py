import argparse
import json
import math
import sys
from pathlib import Path

import matplotlib

if "--preview" in sys.argv:
    matplotlib.use("Agg")

from matplotlib.figure import Figure


matplotlib.rcParams["font.sans-serif"] = ["Microsoft YaHei", "SimHei", "DejaVu Sans"]
matplotlib.rcParams["axes.unicode_minus"] = False
DECODER = json.JSONDecoder()
DEFAULT_FILE = "StageMapCache.example.json"


def read_cache(path):
    with open(path, "r", encoding="utf-8-sig", newline="") as file:
        text = file.read()
    cache = json.loads(text)
    if not isinstance(cache, dict) or not isinstance(cache.get("StageMap"), dict):
        raise ValueError("JSON 中没有顶层 StageMap 对象。")

    stage_map = cache["StageMap"]
    names = ("IdealMatrix", "ErrorMatrix", "IsInWaferMatrix", "IsMatchMatrix")
    matrices = []
    for name in names:
        matrix = stage_map.get(name)
        if not isinstance(matrix, list) or not matrix:
            raise ValueError(f"StageMap.{name} 不是非空矩阵。")
        matrices.append(matrix)

    row_count = len(matrices[0])
    for name, matrix in zip(names, matrices):
        if len(matrix) != row_count:
            raise ValueError(f"StageMap.{name} 的行数与 IdealMatrix 不一致。")

    points = []
    for row_index in range(row_count):
        rows = [matrix[row_index] for matrix in matrices]
        if any(not isinstance(row, list) for row in rows):
            raise ValueError(f"第 {row_index + 1} 行不是数组。")
        if any(len(row) != len(rows[0]) for row in rows):
            raise ValueError(f"第 {row_index + 1} 行的矩阵长度不一致。")
        for column_index, (ideal, error, in_wafer, is_match) in enumerate(zip(*rows)):
            if not isinstance(in_wafer, bool) or not isinstance(is_match, bool):
                raise ValueError(f"第 {row_index + 1} 行第 {column_index + 1} 列的状态不是布尔值。")
            if not (in_wafer and is_match):
                continue
            if not isinstance(ideal, dict) or not isinstance(error, dict):
                raise ValueError(f"第 {row_index + 1} 行第 {column_index + 1} 列不是坐标对象。")
            values = [ideal.get("X"), ideal.get("Y"), error.get("X"), error.get("Y")]
            if any(isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value) for value in values):
                raise ValueError(f"第 {row_index + 1} 行第 {column_index + 1} 列包含无效坐标或误差。")
            points.append((row_index, column_index, *values))

    if not points:
        raise ValueError("没有同时满足 IsInWaferMatrix 和 IsMatchMatrix 的有效点。")
    return text, points


def skip_space(text, index):
    while index < len(text) and text[index] in " \t\r\n":
        index += 1
    return index


def members(text, start):
    index = skip_space(text, start)
    if index >= len(text) or text[index] != "{":
        raise ValueError("预期为 JSON 对象。")
    index = skip_space(text, index + 1)
    while text[index] != "}":
        key, index = DECODER.raw_decode(text, index)
        index = skip_space(text, index)
        if not isinstance(key, str) or text[index] != ":":
            raise ValueError("JSON 对象字段无效。")
        value_start = skip_space(text, index + 1)
        _, value_end = DECODER.raw_decode(text, value_start)
        yield key, value_start, value_end
        index = skip_space(text, value_end)
        if text[index] == ",":
            index = skip_space(text, index + 1)
        elif text[index] != "}":
            raise ValueError("JSON 对象分隔符无效。")


def elements(text, start):
    index = skip_space(text, start)
    if index >= len(text) or text[index] != "[":
        raise ValueError("预期为 JSON 数组。")
    index = skip_space(text, index + 1)
    while text[index] != "]":
        value_start = index
        _, value_end = DECODER.raw_decode(text, value_start)
        yield value_start, value_end
        index = skip_space(text, value_end)
        if text[index] == ",":
            index = skip_space(text, index + 1)
        elif text[index] != "]":
            raise ValueError("JSON 数组分隔符无效。")


def member_span(text, start, name):
    matches = [(value_start, value_end) for key, value_start, value_end in members(text, start) if key == name]
    if len(matches) != 1:
        raise ValueError(f"字段 {name} 不存在或重复。")
    return matches[0]


def apply_x_scale(text, points, percent):
    if not math.isfinite(percent):
        raise ValueError("线性比例误差必须是有限数字。")
    ideal_xs = [point[2] for point in points]
    center = min(ideal_xs) + (max(ideal_xs) - min(ideal_xs)) / 2
    target_points = {(point[0], point[1]): point for point in points}
    stage_start, _ = member_span(text, 0, "StageMap")
    error_start, _ = member_span(text, stage_start, "ErrorMatrix")
    replacements = []
    for row_index, (row_start, _) in enumerate(elements(text, error_start)):
        for column_index, (vector_start, _) in enumerate(elements(text, row_start)):
            point = target_points.get((row_index, column_index))
            if point is None:
                continue
            x_start, x_end = member_span(text, vector_start, "X")
            new_x = (point[2] - center) * percent / 100
            if not math.isfinite(new_x):
                raise ValueError("比例过大，导致误差超出数值范围。")
            replacements.append((x_start, x_end, json.dumps(new_x, allow_nan=False)))
    if len(replacements) != len(points):
        raise ValueError("未能定位全部有效点的 X 误差。")

    parts = []
    cursor = 0
    for start, end, value in replacements:
        parts.extend((text[cursor:start], value))
        cursor = end
    parts.append(text[cursor:])
    return "".join(parts), center, len(replacements)


def save_text(path, text):
    text = text.replace("\r\n", "\n").replace("\r", "\n").replace("\n", "\r\n").rstrip("\r\n")
    with open(path, "w", encoding="utf-8", newline="") as file:
        file.write(text)


def validate_magnification(value):
    if not math.isfinite(value) or value <= 0:
        raise ValueError("显示倍率必须是大于 0 的有限数字。")
    return value


def draw_figure(figure, points, magnification):
    magnification = validate_magnification(magnification)
    figure.clear()
    ax_map = figure.add_subplot(121)
    ax_error = figure.add_subplot(122)
    xs = [point[2] for point in points]
    ys = [point[3] for point in points]
    error_xs = [point[4] for point in points]
    error_ys = [point[5] for point in points]
    extent = max(abs(value) for value in error_xs) or 1
    arrows = ax_map.quiver(
        xs, ys,
        [value * magnification for value in error_xs],
        [value * magnification for value in error_ys],
        error_xs, cmap="coolwarm", norm=matplotlib.colors.Normalize(vmin=-extent, vmax=extent),
        angles="xy", scale_units="xy", scale=1, width=0.002,
        headwidth=3.5, headlength=4.5, headaxislength=4,
    )
    ax_map.scatter(xs, ys, s=3, c="black", alpha=0.5, zorder=3, label="Ideal 起点")
    ax_map.set_title(f"基于 Ideal 的误差向量（显示倍率 {magnification:g}）")
    ax_map.set_xlabel("Ideal X (μm)")
    ax_map.set_ylabel("Ideal Y (μm)")
    end_xs = [x + error * magnification for x, error in zip(xs, error_xs)]
    end_ys = [y + error * magnification for y, error in zip(ys, error_ys)]
    if not all(math.isfinite(value) for value in end_xs + end_ys):
        raise ValueError("显示倍率过大，导致坐标超出数值范围。")
    ax_map.update_datalim(list(zip(end_xs, end_ys)))
    ax_map.autoscale_view()
    ax_map.set_aspect("equal", adjustable="datalim")
    ax_map.grid(alpha=0.2)
    ax_map.legend(loc="best")
    figure.colorbar(arrows, ax=ax_map, shrink=0.8, label="实际 X 误差 (μm)")

    ax_error.scatter(xs, error_xs, s=8, c=error_xs, cmap="coolwarm", vmin=-extent, vmax=extent, alpha=0.65)
    ax_error.axhline(0, color="gray", linewidth=0.8)
    mean_x = sum(xs) / len(xs)
    mean_error = sum(error_xs) / len(error_xs)
    denominator = sum((x - mean_x) ** 2 for x in xs)
    if denominator:
        slope = sum((x - mean_x) * (error - mean_error) for x, error in zip(xs, error_xs)) / denominator
        left, right = min(xs), max(xs)
        ax_error.plot([left, right], [mean_error + slope * (left - mean_x), mean_error + slope * (right - mean_x)],
                      color="black", linewidth=1.2, label=f"线性拟合比例: {slope * 100:.6g}%")
        ax_error.legend(loc="best")
    ax_error.set_title("实际 X 误差与 Ideal X")
    ax_error.set_xlabel("Ideal X (μm)")
    ax_error.set_ylabel("实际 Error X (μm)")
    ax_error.grid(alpha=0.2)
    figure.tight_layout()
    return min(error_xs), max(error_xs)


def run_gui(initial_path, initial_magnification, initial_percent):
    import tkinter as tk
    from tkinter import filedialog, messagebox, ttk
    from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg, NavigationToolbar2Tk

    class Application(tk.Tk):
        def __init__(self):
            super().__init__()
            self.title("StageMap 误差查看与线性误差生成")
            self.geometry("1500x800")
            self.minsize(900, 600)
            self.path = tk.StringVar(value=str(initial_path))
            self.magnification = tk.StringVar(value=str(initial_magnification))
            self.percent = tk.StringVar(value=str(initial_percent))
            self.status = tk.StringVar(value="请选择 StageMapCache JSON 文件。")
            self.text = None
            self.points = None

            path_row = ttk.Frame(self, padding=(10, 10, 10, 3))
            path_row.pack(fill=tk.X)
            ttk.Label(path_row, text="JSON 文件:").pack(side=tk.LEFT)
            ttk.Entry(path_row, textvariable=self.path, state="readonly").pack(side=tk.LEFT, fill=tk.X, expand=True, padx=6)
            ttk.Button(path_row, text="打开文件", command=self.open_file).pack(side=tk.LEFT)

            control_row = ttk.Frame(self, padding=(10, 3))
            control_row.pack(fill=tk.X)
            ttk.Label(control_row, text="向量显示倍率:").pack(side=tk.LEFT)
            entry = ttk.Entry(control_row, textvariable=self.magnification, width=10)
            entry.pack(side=tk.LEFT, padx=6)
            entry.bind("<Return>", lambda event: self.refresh())
            ttk.Button(control_row, text="刷新显示", command=self.refresh).pack(side=tk.LEFT)
            ttk.Label(control_row, text="X 线性比例误差 (%):").pack(side=tk.LEFT, padx=(25, 0))
            ttk.Entry(control_row, textvariable=self.percent, width=10).pack(side=tk.LEFT, padx=6)
            ttk.Button(control_row, text="生成并另存 JSON", command=self.save_scaled).pack(side=tk.LEFT)

            ttk.Label(self, text="箭头从 Ideal 出发；显示倍率只影响左图。右图和颜色显示 JSON 中的实际误差。", padding=(10, 3)).pack(anchor=tk.W)
            self.figure = Figure(figsize=(13, 6), dpi=100)
            self.canvas = FigureCanvasTkAgg(self.figure, master=self)
            toolbar = NavigationToolbar2Tk(self.canvas, self, pack_toolbar=False)
            toolbar.update()
            toolbar.pack(side=tk.BOTTOM, fill=tk.X)
            ttk.Label(self, textvariable=self.status, padding=(10, 5)).pack(side=tk.BOTTOM, anchor=tk.W)
            self.canvas.get_tk_widget().pack(fill=tk.BOTH, expand=True)
            if initial_path.is_file():
                self.load(initial_path)

        def open_file(self):
            selected = filedialog.askopenfilename(
                parent=self, title="选择 StageMapCache JSON 文件",
                filetypes=[("所有文件", "*.*"), ("JSON 文件", "*.json")],
                initialdir=str(Path(self.path.get()).parent) if self.path.get() else None,
            )
            if selected:
                self.load(Path(selected))

        def load(self, path):
            try:
                text, points = read_cache(path)
                magnification = validate_magnification(float(self.magnification.get()))
                minimum, maximum = draw_figure(self.figure, points, magnification)
            except (ValueError, OSError, TypeError, KeyError, IndexError) as error:
                messagebox.showerror("无法打开 JSON", str(error), parent=self)
                return
            self.text, self.points = text, points
            self.path.set(str(path))
            self.status.set(f"有效点: {len(points)}；实际 X 误差范围: {minimum:.4f} 至 {maximum:.4f} μm")
            self.canvas.draw_idle()

        def refresh(self):
            if self.points is None:
                return
            try:
                magnification = validate_magnification(float(self.magnification.get()))
                minimum, maximum = draw_figure(self.figure, self.points, magnification)
            except ValueError as error:
                messagebox.showerror("显示倍率无效", str(error), parent=self)
                return
            self.status.set(f"有效点: {len(self.points)}；实际 X 误差范围: {minimum:.4f} 至 {maximum:.4f} μm")
            self.canvas.draw_idle()

        def save_scaled(self):
            if self.text is None or self.points is None:
                messagebox.showinfo("尚未打开文件", "请先选择 StageMapCache JSON 文件。", parent=self)
                return
            try:
                percent = float(self.percent.get())
                result, center, count = apply_x_scale(self.text, self.points, percent)
            except (ValueError, TypeError, IndexError) as error:
                messagebox.showerror("无法生成误差", str(error), parent=self)
                return
            current = Path(self.path.get())
            selected = filedialog.asksaveasfilename(
                parent=self, title="另存线性误差 JSON", initialdir=str(current.parent),
                initialfile=f"{current.stem}_x_scale.json", defaultextension=".json",
                filetypes=[("JSON 文件", "*.json"), ("所有文件", "*.*")],
            )
            if not selected:
                return
            try:
                save_text(selected, result)
            except OSError as error:
                messagebox.showerror("保存失败", str(error), parent=self)
                return
            self.load(Path(selected))
            messagebox.showinfo("已生成", f"已修改 {count} 个有效点的 X 误差。\n公式: ErrorX = (IdealX - {center:.6f}) × {percent:g}%\n已打开新文件。", parent=self)

    Application().mainloop()


def main():
    parser = argparse.ArgumentParser(description="查看 StageMap 误差，或生成 X 方向线性比例误差。")
    default_path = Path(__file__).with_name(DEFAULT_FILE)
    if not default_path.is_file():
        default_path = Path(__file__).with_name("Json_1")
    parser.add_argument("--input", type=Path, default=default_path, help="StageMapCache JSON 路径")
    parser.add_argument("--magnification", type=float, default=5, help="误差向量显示倍率，默认 5")
    parser.add_argument("--percent", type=float, default=1, help="生成 X 线性误差时使用的百分比，默认 1")
    parser.add_argument("--generate", type=Path, help="按比例生成 JSON 到指定路径")
    parser.add_argument("--preview", type=Path, help="将误差图保存为 PNG")
    args = parser.parse_args()
    if args.generate is not None:
        text, points = read_cache(args.input)
        result, center, count = apply_x_scale(text, points, args.percent)
        save_text(args.generate, result)
        print(f"已修改 {count} 个有效点，基准 Ideal X = {center:.6f}，比例 = {args.percent:g}%")
    elif args.preview is not None:
        _, points = read_cache(args.input)
        figure = Figure(figsize=(14, 6), dpi=140)
        draw_figure(figure, points, args.magnification)
        figure.savefig(args.preview, dpi=140)
        print(f"已保存预览: {args.preview}")
    else:
        run_gui(args.input, args.magnification, args.percent)


if __name__ == "__main__":
    main()