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

using Luban.Diagnostics;
using Luban.RawDefs;
using Luban.Schema;
using Luban.Utils;

namespace Luban.Defs;

public abstract class DefTypeBase
{
    public DefAssembly Assembly { get; set; }

    /// <summary>Schema definition origin (file + optional excel sheet).</summary>
    public SchemaSource Source { get; set; }

    public string Name { get; set; }

    public string Namespace { get; set; }

    public string FullName => TypeUtil.MakeFullName(Namespace, Name);

    public string GoFullName => TypeUtil.MakeGoFullName(Namespace, Name);

    public string NamespaceWithTopModule => TypeUtil.MakeNamespace(GenerationContext.Current.TopModule, Namespace);

    public string FullNameWithTopModule => TypeUtil.MakeNamespace(GenerationContext.Current.TopModule, FullName);

    public List<string> Groups { get; set; }

    public string Comment { get; protected set; }

    public Dictionary<string, string> Tags { get; protected set; }

    /// <summary>
    /// 生成代码时额外派生的 trait（逗号分隔），由 schema 里的 <c>derives</c> 属性声明。
    /// 只给代码模板用，不参与数据校验；为空时由代码目标自己的默认值决定。
    /// </summary>
    public string Derives { get; set; }

    public List<TypeMapper> TypeMappers { get; protected set; }

    public bool HasTag(string attrName)
    {
        return Tags != null && Tags.ContainsKey(attrName);
    }

    public string GetTag(string attrName)
    {
        return Tags != null && Tags.TryGetValue(attrName, out var value) ? value : null;
    }

    public virtual void PreCompile()
    {
        if (Groups != null && Groups.Count > 0)
        {
            LubanConfig c = GenerationContext.GlobalConf;
            if (Groups.Contains("*"))
            {
                Groups.Clear();
                Groups.AddRange(c.Groups.SelectMany(g => g.Names));
            }
            else
            {
                foreach (var g in Groups)
                {
                    if (c.Groups.All(gg => !gg.Names.Contains(g)))
                    {
                        throw new LubanException(Source, "error.def.type.group_not_found", FullName, g);
                    }
                }
            }
        }
    }

    public abstract void Compile();

    public virtual void PostCompile() { }
}
