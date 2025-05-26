import "./assets/css/style.css";
import "viewerjs/dist/viewer.css";

import Alpine from "alpinejs";
import Viewer from "viewerjs";
import html2canvas from "html2canvas";

window.Alpine = Alpine;
window.colorsMap = [
    "#31193F",
    "#321B44",
    "#331D4A",
    "#341F50",
    "#352256",
    "#36245C",
    "#372661",
    "#382867",
    "#392B6D",
    "#3A2D73",
    "#3B2F78",
    "#3C327E",
    "#3D3483",
    "#3E3689",
    "#3F388E",
    "#403B94",
    "#413D99",
    "#423F9E",
    "#4342A3",
    "#4444A8",
    "#4546AD",
    "#4648B2",
    "#464BB7",
    "#474DBC",
    "#484FC0",
    "#4852C4",
    "#4954C9",
    "#4A56CD",
    "#4A59D1",
    "#4B5BD5",
    "#4B5DD9",
    "#4C5FDC",
    "#4C62E0",
    "#4C64E3",
    "#4C66E6",
    "#4D69E9",
    "#4D6BEC",
    "#4D6DEF",
    "#4D6FF1",
    "#4D72F3",
    "#4D74F5",
    "#4D76F7",
    "#4C79F9",
    "#4C7BFA",
    "#4C7DFC",
    "#4B80FD",
    "#4B82FE",
    "#4A84FE",
    "#4A87FF",
    "#4989FF",
    "#488BFF",
    "#488EFF",
    "#4790FE",
    "#4692FD",
    "#4594FC",
    "#4397FB",
    "#4299F9",
    "#419BF8",
    "#409EF6",
    "#3EA0F4",
    "#3DA2F2",
    "#3CA5EF",
    "#3AA7ED",
    "#39A9EA",
    "#37ABE7",
    "#36AEE4",
    "#34B0E1",
    "#33B2DD",
    "#31B4DA",
    "#30B6D6",
    "#2EB9D3",
    "#2DBBCF",
    "#2BBDCB",
    "#2ABFC7",
    "#28C1C3",
    "#27C3BF",
    "#25C5BB",
    "#24C7B7",
    "#23C9B2",
    "#21CBAE",
    "#20CDAA",
    "#1FCFA6",
    "#1ED1A1",
    "#1DD39D",
    "#1CD599",
    "#1BD794",
    "#1BD990",
    "#1ADA8C",
    "#19DC87",
    "#19DE83",
    "#18DF7F",
    "#18E17B",
    "#18E377",
    "#18E473",
    "#18E66F",
    "#18E76B",
    "#19E967",
    "#19EA64",
    "#1AEB60",
    "#1AED5D",
    "#1BEE5A",
    "#1CEF56",
    "#1EF154",
    "#1FF251",
    "#20F34E",
    "#22F44B",
    "#24F549",
    "#26F647",
    "#28F745",
    "#2AF743",
    "#2DF841",
    "#2FF93F",
    "#32FA3D",
    "#35FA3C",
    "#37FB3A",
    "#3AFC39",
    "#3DFC38",
    "#40FD37",
    "#44FD36",
    "#47FD35",
    "#4AFE34",
    "#4EFE33",
    "#51FE33",
    "#55FE32",
    "#59FF32",
    "#5CFF31",
    "#60FF31",
    "#64FF31",
    "#68FF31",
    "#6CFF31",
    "#70FE31",
    "#73FE31",
    "#77FE31",
    "#7BFE31",
    "#7FFD31",
    "#83FD31",
    "#87FC32",
    "#8BFC32",
    "#8FFB32",
    "#93FB33",
    "#97FA33",
    "#9BF933",
    "#9FF834",
    "#A3F834",
    "#A6F735",
    "#AAF635",
    "#AEF536",
    "#B1F436",
    "#B5F337",
    "#B8F237",
    "#BCF038",
    "#BFEF38",
    "#C2EE39",
    "#C6ED3A",
    "#C9EB3A",
    "#CCEA3A",
    "#CEE83B",
    "#D1E73B",
    "#D4E53C",
    "#D7E33C",
    "#D9E23D",
    "#DCE03D",
    "#DEDE3D",
    "#E0DC3E",
    "#E2DA3E",
    "#E4D93E",
    "#E6D73E",
    "#E8D53F",
    "#EAD33F",
    "#ECD13F",
    "#EECE3F",
    "#EFCC40",
    "#F1CA40",
    "#F2C840",
    "#F3C640",
    "#F5C440",
    "#F6C140",
    "#F7BF40",
    "#F8BD40",
    "#F9BA40",
    "#FAB840",
    "#FAB640",
    "#FBB340",
    "#FBB140",
    "#FCAE40",
    "#FCAC40",
    "#FDA940",
    "#FDA73F",
    "#FDA43F",
    "#FDA23F",
    "#FD9F3F",
    "#FD9D3E",
    "#FD9A3E",
    "#FD983E",
    "#FD953D",
    "#FC933D",
    "#FC903D",
    "#FB8D3C",
    "#FB8B3C",
    "#FA883B",
    "#F9863B",
    "#F8833A",
    "#F8813A",
    "#F77E39",
    "#F67C39",
    "#F47938",
    "#F37637",
    "#F27436",
    "#F17136",
    "#EF6F35",
    "#EE6C34",
    "#EC6A33",
    "#EB6733",
    "#E96532",
    "#E76231",
    "#E66030",
    "#E45D2F",
    "#E25B2E",
    "#E0582D",
    "#DE562C",
    "#DC542B",
    "#DA512A",
    "#D84F29",
    "#D64C28",
    "#D34A27",
    "#D14726",
    "#CF4525",
    "#CC4224",
    "#CA4022",
    "#C83E21",
    "#C53B20",
    "#C3391F",
    "#C0361E",
    "#BD341C",
    "#BB321B",
    "#B82F1A",
    "#B62D19",
    "#B32A17",
    "#B02816",
    "#AD2615",
    "#AB2314",
    "#A82112",
    "#A51E11",
    "#A21C10",
    "#9F1A0E",
    "#9D170D",
    "#9A150C",
    "#97120A",
    "#941009",
    "#910E08",
    "#8E0B06",
    "#8B0905",
    "#880704",
    "#850402",
    "#820201",
    "#800000"
];

window.Heat2DColorMap = function (maxValue, minValue, value) {
    let index = Math.floor((value - minValue) / (maxValue - minValue).toFixed(4) * (colorsMap.length - 1));
    return colorsMap[index];
}
window.addEventListener("DOMContentLoaded", (event) => {
    const navLinks = document.querySelectorAll('.menu-link-color');
    const sections = document.querySelectorAll('.menu-link-section');
    let isClick = false; // 新增标志变量

    const horizontalHandle = document.getElementById('horizontal-handle');
    const horizontalSplitter = document.getElementById('horizontal-splitter');
    const leftPanel = document.getElementById('left-panel');
    const menuUlWidth = leftPanel.offsetWidth;
    const menuUlList = document.querySelectorAll('.menu-ul-display');
    let isHorizontalDragging = false;

    horizontalHandle.addEventListener('mousedown', () => {
        isHorizontalDragging = true;
        document.body.classList.add('cursor-col-resize', 'select-none');
    });

    document.addEventListener('mousemove', (e) => {
        if (isHorizontalDragging) {
            const containerRect = horizontalSplitter.getBoundingClientRect();

            const leftWidth = (e.clientX / containerRect.width) * 100;
            // 限制最小和最大宽度
            const clampedWidth = Math.max(2, Math.min(99, leftWidth));
            leftPanel.style.width = `${clampedWidth}%`;
            if (e.clientX > menuUlWidth) {
                menuUlList.forEach(link => {
                    link.classList.remove('w-64');
                });
            }
            if (e.clientX <= menuUlWidth) {
                menuUlList.forEach(link => {
                    link.classList.add('w-64');
                });
            }
        }

    });
    document.addEventListener('mouseup', () => {
        isHorizontalDragging = false;
        document.body.classList.remove('cursor-col-resize', 'select-none');
    });

    const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (entry.isIntersecting) { // 增加条件判断
                const id = entry.target.getAttribute('id');
                navLinks.forEach(link => {
                    if (link.getAttribute('href').split('#')[1] === id) {
                        link.classList.replace('menu-link-color', 'menu-link-style');
                        if (!isClick) {
                            link.scrollIntoView({behavior: 'auto', block: 'center'});
                        }
                        TreeViewIsFalse(link);
                    } else {
                        link.classList.replace('menu-link-style', 'menu-link-color');
                    }
                });
            }
        });
    }, {rootMargin: "-20% 0% -75% 0%"}); // 可以调整这个值以适应实际布局和设计需求

    // 观察每个 section 元素以及其所有子项
    sections.forEach(section => {
        observer.observe(section);
    });
    // 为每个 navLink 添加点击事件监听器
    navLinks.forEach(link => {
        link.addEventListener('click', () => {
            isClick = true; // 设置标志变量为 true
            setTimeout(() => {
                isClick = false;
            }, 100); // 点击后 100 毫秒重置标志变量
        });
    });

    function TreeViewIsFalse(trigger) {
        const ulElement = trigger.parentElement.parentElement;
        if (ulElement && ulElement.nodeName === 'UL') {
            if (ulElement.getAttribute('data-menu-visible') === 'false') {
                ulElement.setAttribute('data-menu-visible', 'true');
            }

            const svgContainer = ulElement.parentElement.firstElementChild;
            if (svgContainer && svgContainer.firstElementChild) {
                const svgElement = svgContainer.firstElementChild.firstElementChild;
                if (svgElement.getAttribute('data-menu-svg-rotate') === 'false') {
                    svgElement.setAttribute('data-menu-svg-rotate', 'true');
                }
            }

            if (svgContainer && svgContainer.nodeName === 'A') {
                TreeViewIsFalse(svgContainer);
            }
        }
    }
});

Alpine.data("viewModel", () => {
    return ({
        toggleAllMenu: true,
        toggleAllImage: true,
        toggleAllLog: true,

        toggleMenuVisibility(event, trigger) {
            event.preventDefault();

            const ul = trigger.parentElement.parentElement.querySelector("ul[data-menu-visible]");
            const svg = trigger.parentElement.querySelector("svg[data-menu-svg-rotate]");

            const isLiVisible = ul.getAttribute("data-menu-visible") === "true";
            ul.setAttribute("data-menu-visible", (!isLiVisible).toString());

            const isSvgRotated = svg.getAttribute("data-menu-svg-rotate") === "true";
            svg.setAttribute("data-menu-svg-rotate", (!isSvgRotated).toString());
        },
        toggleAllMenuVisibility(nav) {
            this.toggleAllMenu = !this.toggleAllMenu;

            const allUlItems = nav.querySelectorAll("ul[data-menu-visible]");
            const allUlSvgItems = nav.querySelectorAll("svg[data-menu-svg-rotate]");

            allUlItems.forEach((ul) => ul.setAttribute("data-menu-visible", (this.toggleAllMenu).toString()));
            allUlSvgItems.forEach((ulSvg) => ulSvg.setAttribute("data-menu-svg-rotate", (this.toggleAllMenu).toString()));
        },
        toggleLogVisibility(event, trigger) {
            event.preventDefault();

            const div = trigger.parentElement.querySelector("div[data-log-visible]");
            const svg = trigger.querySelector("svg[data-log-svg-rotate]");

            const isLiVisible = div.getAttribute("data-log-visible") === "true";
            div.setAttribute("data-log-visible", (!isLiVisible).toString());

            const isSvgRotated = svg.getAttribute("data-log-svg-rotate") === "true";
            svg.setAttribute("data-log-svg-rotate", (!isSvgRotated).toString());
        },
        toggleAllLogVisibility(body) {
            this.toggleAllLog = !this.toggleAllLog;

            const allDivItems = body.querySelectorAll("div[data-log-visible]");
            const allDivSvgItems = body.querySelectorAll("svg[data-log-svg-rotate]");

            allDivItems.forEach((div) => div.setAttribute("data-log-visible", (this.toggleAllLog).toString()));
            allDivSvgItems.forEach((divSvg) => divSvg.setAttribute("data-log-svg-rotate", (this.toggleAllLog).toString()));
        },
        openViewer(trigger, fileName) {
            const clone = trigger.cloneNode(true);
            clone.style.position = "absolute";
            clone.style.top = "-999999px";
            document.body.appendChild(clone);
            clone.style.transform = "";
            clone.style.transformOrigin = "";
            html2canvas(clone).then(canvas => {
                document.body.removeChild(clone);
                const img = new Image();
                img.src = canvas.toDataURL("image/png");
                const viewer = new Viewer(img, {
                    inline: false,
                    navbar: false,
                    title: false,
                    toolbar: {
                        zoomIn: true,
                        zoomOut: true,
                        oneToOne: true,
                        reset: true,
                        rotateLeft: true,
                        rotateRight: true,
                        flipHorizontal: true,
                        flipVertical: true,
                        download: function () {
                            const link = document.createElement("a");
                            link.href = img.src;
                            link.download = `${fileName}.png`;
                            link.click();
                            URL.revokeObjectURL(link.href);
                        },
                    },
                    hidden: function () {
                        URL.revokeObjectURL(img.src);
                        img.src = ""; // 释放图像资源
                    }
                });
                viewer.show();
            }).catch(function (error) {
                console.error('html2canvas error:', error);
                document.body.removeChild(clone);
            });
        },
    });
})

Alpine.start();