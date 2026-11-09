using Luban;
using Luban.Defs;
using Scriban.Runtime;

namespace Luban.Bevy.TemplateExtensions;

/// <summary>
/// rust-bevy 目标专属的模板函数：生成的行/枚举要带哪些 derive，由项目自己决定。
///
/// 两处可以声明，**类型上写的优先**：
/// <list type="number">
/// <item>schema 里给单个类型声明：<c>&lt;bean name="X" derives="Clone,PartialEq"&gt;</c>、
/// <c>&lt;enum name="Y" derives="Clone,Copy"&gt;</c>；</item>
/// <item>全局默认用 <c>-x</c> 选项：<c>beanExtraDerives</c> / <c>enumExtraDerives</c> /
/// <c>tableExtraDerives</c>（<c>codeTarget.rust-bevy.</c> 前缀可省略，写裸名即全局生效）。</item>
/// </list>
/// 类型没写 <c>derives</c> 时用 <c>-x</c> 的值；两处都没写就只有 <c>Debug</c>（旧版行为）。
/// <c>Debug</c> 与 <c>atom_datatables::serde::Deserialize</c> 由模板固定，不参与配置 ——
/// 它们是解码与打印的硬需求，去掉生成物就编译不过。
/// </summary>
public class RustBevyTemplateExtension : ScriptObject
{
    private const string Family = "codeTarget.rust-bevy";

    private static string GetExtraDerives(string optionName)
    {
        // useGlobalIfNotExits = true：优先读 codeTarget.rust-bevy.<name>，没有再退回裸名。
        return EnvManager.Current.GetOptionOrDefault(Family, optionName, true, "").Trim();
    }

    /// <summary>
    /// 单个类型自己的声明优先；没写就退回全局默认。
    /// </summary>
    private static string PickExtra(string optionName, string perTypeDerives)
    {
        return string.IsNullOrWhiteSpace(perTypeDerives) ? GetExtraDerives(optionName) : perTypeDerives.Trim();
    }

    private static string Build(string baseDerive, string extraDerives, string? tailDerive = null)
    {
        var parts = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        void Push(string? derive)
        {
            if (string.IsNullOrWhiteSpace(derive))
            {
                return;
            }
            var name = derive.Trim();
            if (seen.Add(name))
            {
                parts.Add(name);
            }
        }

        Push(baseDerive);
        foreach (var extra in extraDerives.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            Push(extra);
        }
        Push(tailDerive);
        return string.Join(", ", parts);
    }

    /// <summary>
    /// 表行（bean）的 derive 列表：<c>Debug</c> + 项目声明 + <c>Deserialize</c>。
    /// </summary>
    public static string BeanDerives(DefBean bean)
    {
        return Build("Debug", PickExtra("beanExtraDerives", bean?.Derives),
            "atom_datatables::serde::Deserialize");
    }

    /// <summary>
    /// 普通枚举的 derive 列表：<c>Debug</c> + 项目声明 + <c>Hash, Eq, PartialEq, Deserialize</c>。
    /// </summary>
    public static string EnumDerives(DefEnum en)
    {
        return Build("Debug", PickExtra("enumExtraDerives", en?.Derives),
            "Hash, Eq, PartialEq, atom_datatables::serde::Deserialize");
    }

    /// <summary>
    /// bitflags 枚举的 derive 列表：<c>Debug</c> + 项目声明 + <c>Hash, Eq, PartialEq</c>。
    /// </summary>
    public static string FlagEnumDerives(DefEnum en)
    {
        return Build("Debug", PickExtra("enumExtraDerives", en?.Derives), "Hash, Eq, PartialEq");
    }

    /// <summary>
    /// 表容器（<c>TbXxx</c>）的 derive 列表。默认只有 <c>Debug</c>。
    /// 表不是 schema 里的类型（由 <c>&lt;table&gt;</c> 生成），所以只能用全局选项。
    /// </summary>
    public static string TableDerives()
    {
        return Build("Debug", GetExtraDerives("tableExtraDerives"));
    }
}
