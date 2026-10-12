# 六日英雄 WebGL 部署说明

## 一、要上传哪些文件

把 WebGL 构建目录里的以下上传到网站根目录：

- `index.html`
- `Build/`（WebGL.loader.js、WebGL.framework.js.br、WebGL.wasm.br、WebGL.data.br）
- `StreamingAssets/`（Combat / Shelter / Events / Endings 的 json，以及 `backstory.mp4`）
- `TemplateData/`

> StreamingAssets 必须一起上传：游戏内容（卡牌 / 幸存者 / 事件 / 结局）和开场视频都在里面，
> 缺了会出现“点开始游戏没反应”或“视频不播”。

## 二、本目录的文件怎么用

| 文件 | 用途 |
| --- | --- |
| `web.config` | IIS：放到网站根目录，和 index.html 同级。需要 URL Rewrite 模块，详见文件头注释。 |
| `nginx.conf` | nginx：改掉里面的 `root` 指向构建目录即可使用（或把 server 段并入你现有的配置）。 |

## 三、Brotli 的 Content-Encoding 到底在配什么

Unity 勾选 Brotli 压缩后，产物名字带 `.br`：`WebGL.data.br`、`WebGL.wasm.br`、`WebGL.framework.js.br`。
但浏览器里的加载器请求的是 **不带 `.br` 的名字**：

    Build/WebGL.data
    Build/WebGL.wasm
    Build/WebGL.framework.js

所以服务器要做两件事，缺一不可：

1. **改写请求**：收到 `WebGL.data` 时实际返回 `WebGL.data.br`（nginx 用 `try_files`，IIS 用 URL Rewrite 入站规则）。
2. **声明编码**：响应头加 `Content-Encoding: br`（nginx 用 `add_header`，IIS 用出站规则）。
   同时 **Content-Type 必须是原始扩展名对应的类型**——如果 `framework.js.br` 以
   `application/octet-stream` 返回，浏览器会以“MIME type 可执行性”问题拒绝执行，游戏直接白屏。

浏览器收到 `Content-Encoding: br` 后会自己解压，所以磁盘上留压缩文件、网络上少传字节，这是这套配置的目的。

## 四、如果你嫌麻烦

最简单的替代方案：Unity 的 Player Settings  Publishing Settings  Compression Format
改成 **Disabled**（或 Gzip），然后不用任何改写规则，普通静态托管 + 服务器自带的 gzip/brotli 动态压缩即可。
代价是首次下载体积更大，服务器端压缩会多耗一点 CPU。

## 五、上线前自检清单

1. F12  Network，筛选 `WebGL`：
   - `WebGL.data` 状态 200，响应头有 `Content-Encoding: br`；
   - `WebGL.framework.js` 的 `Content-Type` 是 `application/javascript`，不是 `octet-stream`；
   - `WebGL.wasm` 的 `Content-Type` 是 `application/wasm`。
2. Console 里不应出现 `Refused to execute script ... MIME type` 或
   `Incorrect response MIME type application/wasm`。
3. 请求 `StreamingAssets/backstory.mp4` 返回 200 且带 `Accept-Ranges: bytes`
   （没有 Range 支持，21MB 的开场视频要整段下完才开始播）。
4. 进入游戏后点“开始游戏”能正常进入剧情，视频能出画面。

## 六、别忘了

- 若改用自签/无 HTTPS 的站点，Unity WebGL 在部分浏览器会被限制，正式对外建议上 HTTPS。