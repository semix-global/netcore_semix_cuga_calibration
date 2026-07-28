namespace Net.Utilities.SourceGenerators.Calibration;

internal static class SourceGeneratorHelper
{
    /// <summary>
    /// 字符串转换为Pascal命名字符串<br/>
    /// <remark>
    /// "testName" => "TestName"<br/>
    /// "test Name" => "TestName"<br/>
    /// "test_Name" => "TestName"<br/>
    /// " test Name" => "TestName"<br/>
    /// "123testName" => "TestName"<br/>
    /// "%testName" => "TestName"<br/>
    /// "Who am I?" => "WhoAmI"<br/>
    /// "Hello|Who|Am|I?" => "HelloWhoAmI"
    /// </remark>
    /// </summary>
    /// <param name="name">字符串</param>
    /// <returns>Pascal命名</returns>
    internal static string ToPascalCaseName(string name)
    {
        var span = name.AsSpan();
        if (span.IsEmpty) return string.Empty;

        var resultSize = 0;
        foreach (var @char in span)
        {
            switch (resultSize)
            {
                case 0 when char.IsLetter(@char):
                case > 0 when char.IsLetterOrDigit(@char): // 首字母忽略非字母和数字, 后面的字符只保留字母和数字
                    resultSize++;
                    break;
            }
        }

        Span<char> result = stackalloc char[resultSize];

        var written = 0;
        var nextUpper = true;

        foreach (var @char in span)
        {
            if ((written == 0 && char.IsLetter(@char) == false) || char.IsLetterOrDigit(@char) == false) // 首字母忽略非字母和数字, 后面的字符只保留字母和数字
            {
                nextUpper = true;
                continue;
            }

            if (nextUpper)
                result[written++] = char.ToUpper(@char);
            else
                result[written++] = @char;

            nextUpper = false;
        }

        return result.ToString();
    }
}