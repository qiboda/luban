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

## derive 配置

生成的行/枚举带哪些 derive 由**项目自己**用 `-x` 选项决定，不写死在模板里
（实现见 `TemplateExtensions/RustBevyTemplateExtension.cs`）：

| 选项 | 默认值 | 作用 |
|---|---|---|
| `beanExtraDerives` | 空 | 表行（bean）额外派生的 trait |
| `enumExtraDerives` | 空 | 枚举（含 bitflags）额外派生的 trait |
| `tableExtraDerives` | 空 | 表容器 `TbXxx` 额外派生的 trait |

```sh
dotnet Luban.dll -t mygame -c rust-bevy -d bin --conf luban.conf \
  -x beanExtraDerives=Clone,PartialEq,Serialize \
  -x enumExtraDerives=Clone,Copy
```

- `Debug` 与 `atom_datatables::serde::Deserialize` 固定生成，不参与配置（解码与打印的硬需求）。
- 默认全部为空：不传 `-x` 时生成物与旧版完全一致，不会凭空多出 `Clone` / `PartialEq`。
- 值留空即关掉（`-x beanExtraDerives=`）；项目按需打开，例如 dimwatch 的 `gen_data.ps1` 传的就是 `Clone,PartialEq` / `Clone,Copy`。
- 选项名前缀可省略（裸名全局生效）；要只对某个 target 生效就写全名
  `codeTarget.rust-bevy.beanExtraDerives`。
- 加 `Serialize` 之类会要求字段类型也实现它，生成物编译不过时先确认这一条。

生成代码的约定（由 `atom_datatables` 一侧兑现）：

| 生成项 | 含义 |
|---|---|
| `impl Table for TbX` | `FILE` 是 `<table>.bytes`，`parse` 解码二进制 |
| `pub const SPEC: TableSpec` | 文件列表 + `load_all` |
| `pub fn load_all(..)` | 逐表解析并写入 `Tables` |

`crate::` 前缀意味着生成文件应作为 crate 根（`src/lib.rs`）使用；
以模块方式引入时需要在外层 `pub use` 把类型提到根上。
