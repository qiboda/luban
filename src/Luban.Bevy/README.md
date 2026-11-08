# Luban.Bevy

Luban 的 `rust-bevy` 代码目标：把数据表生成为「可直接接入 Bevy 资产管线」的 Rust 代码。

与 `rust-bin` 的区别：

- 不生成 `macros` proc-macro crate（枚举的 `From<i32>` 由模板直接生成）；
- 生成代码只依赖 `atom_datatables`，不使用 Luban 自带的 `luban_lib`；
- 每张表实现 `atom_datatables::Table`，并把「文件名 → 解析函数」导出为
  `atom_datatables::TableSpec`，交给 `DataTablePlugin` 异步加载；
- 暂不支持多态 bean（抽象 / 继承），遇到时生成 `compile_error!`。

## 使用

```sh
dotnet Luban.dll -t <target> -c rust-bevy -d bin --conf <luban.conf>
```

模板位于 `Templates/rust-bevy/`（`lib.sbn` / `mod.sbn` / `toml.sbn`），
构建时通过 `CopyToOutputDirectory` 复制到输出目录的 `Templates/` 下。

生成代码的约定（由 `atom_datatables` 一侧兑现）：

| 生成项 | 含义 |
|---|---|
| `impl Table for TbX` | `FILE` 是 `<table>.bytes`，`parse` 解码二进制 |
| `pub const SPEC: TableSpec` | 文件列表 + `load_all` |
| `pub fn load_all(..)` | 逐表解析并写入 `Tables` |

`crate::` 前缀意味着生成文件应作为 crate 根（`src/lib.rs`）使用；
以模块方式引入时需要在外层 `pub use` 把类型提到根上。
