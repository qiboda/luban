// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Luban.CodeTarget;
using Luban.CSharp.CodeTarget;

namespace Luban.Bevy.CodeTarget;

/// <summary>
/// ATOM 的 Bevy 数据表代码目标。
///
/// 独立成 <c>Luban.Bevy</c> 项目，而不是改 <c>Luban.Rust</c>：上游 Rust 目标保持原样，
/// Bevy 集成（atom_datatables 运行时、Table 接口、TableSpec）只活在这一层。
///
/// 与内置 <c>rust-bin</c> 的区别：
/// <list type="bullet">
/// <item>不生成 <c>macros</c> 过程宏 crate —— 枚举自带 <c>From&lt;i32&gt;</c>，数据表运行时由
/// <c>atom_datatables</c> 提供。</item>
/// <item>生成的代码只依赖 <c>atom_datatables</c>：每张表实现 <c>atom_datatables::Table</c>，
/// 并额外导出一个 <c>TableSpec</c>（<c>SPEC</c>）供 Bevy 资产管线按需加载原始字节。</item>
/// </list>
///
/// 多态 bean（继承 / 抽象）暂不支持，模板会直接输出 <c>compile_error!</c>，
/// 避免生成一份“看起来能编译、实际用不了”的代码。
/// </summary>
[CodeTarget("rust-bevy")]
public class RustBevyCodeTarget : RustBinCodeTarget
{
    protected override void GenerateMacros(GenerationContext ctx, OutputFileManifest manifest)
    {
        // rust-bevy 不生成 macros crate：生成的枚举已经带 From<i32>，
        // 运行时的表接口（Table/TableSpec/Tables）在 atom_datatables 里。
    }
}
