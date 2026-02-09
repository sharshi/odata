namespace OData.Testing;

/// <summary>
/// Constants for OData test categories. Used to organize and selectively run test permutations.
/// </summary>
public static class ODataTestCategory
{
    public static class Filter
    {
        public const string Comparison = "Filter.Comparison";
        public const string Logical = "Filter.Logical";
        public const string Arithmetic = "Filter.Arithmetic";
        public const string StringFunctions = "Filter.StringFunctions";
        public const string DateFunctions = "Filter.DateFunctions";
        public const string MathFunctions = "Filter.MathFunctions";
        public const string Lambda = "Filter.Lambda";
        public const string Null = "Filter.Null";
        public const string Enum = "Filter.Enum";
        public const string In = "Filter.In";
        public const string SpecialCharacters = "Filter.SpecialCharacters";
        public const string NavigationProperty = "Filter.NavigationProperty";
    }

    public static class Select
    {
        public const string Single = "Select.Single";
        public const string Multiple = "Select.Multiple";
        public const string NavigationProperty = "Select.NavigationProperty";
        public const string NestedComplex = "Select.NestedComplex";
        public const string Wildcard = "Select.Wildcard";
    }

    public static class Expand
    {
        public const string Single = "Expand.Single";
        public const string Multiple = "Expand.Multiple";
        public const string NestedFilter = "Expand.NestedFilter";
        public const string NestedSelect = "Expand.NestedSelect";
        public const string NestedOrderBy = "Expand.NestedOrderBy";
        public const string NestedTop = "Expand.NestedTop";
        public const string NestedSkip = "Expand.NestedSkip";
        public const string NestedCount = "Expand.NestedCount";
        public const string MultiLevel = "Expand.MultiLevel";
        public const string WithLevels = "Expand.WithLevels";
        public const string CombinedNested = "Expand.CombinedNested";
        public const string Wildcard = "Expand.Wildcard";
    }

    public static class OrderBy
    {
        public const string SingleAsc = "OrderBy.SingleAsc";
        public const string SingleDesc = "OrderBy.SingleDesc";
        public const string Multiple = "OrderBy.Multiple";
        public const string NavigationProperty = "OrderBy.NavigationProperty";
        public const string ByCount = "OrderBy.ByCount";
        public const string ByExpression = "OrderBy.ByExpression";
    }

    public static class Paging
    {
        public const string Top = "Paging.Top";
        public const string Skip = "Paging.Skip";
        public const string TopAndSkip = "Paging.TopAndSkip";
        public const string TopZero = "Paging.TopZero";
        public const string SkipBeyondCount = "Paging.SkipBeyondCount";
    }

    public static class Count
    {
        public const string InlineCount = "Count.InlineCount";
        public const string CountWithFilter = "Count.CountWithFilter";
        public const string CountWithPaging = "Count.CountWithPaging";
        public const string CountInFilter = "Count.CountInFilter";
    }

    public static class Apply
    {
        public const string AggregateSum = "Apply.AggregateSum";
        public const string AggregateMin = "Apply.AggregateMin";
        public const string AggregateMax = "Apply.AggregateMax";
        public const string AggregateAvg = "Apply.AggregateAvg";
        public const string AggregateCount = "Apply.AggregateCount";
        public const string AggregateCountDistinct = "Apply.AggregateCountDistinct";
        public const string GroupBy = "Apply.GroupBy";
        public const string GroupByWithAggregate = "Apply.GroupByWithAggregate";
        public const string FilterTransformation = "Apply.FilterTransformation";
        public const string MultipleAggregates = "Apply.MultipleAggregates";
    }

    public static class Compute
    {
        public const string Basic = "Compute.Basic";
        public const string InFilter = "Compute.InFilter";
        public const string InOrderBy = "Compute.InOrderBy";
        public const string InSelect = "Compute.InSelect";
    }

    public static class Combination
    {
        public const string FilterAndSelect = "Combination.FilterAndSelect";
        public const string FilterAndOrderBy = "Combination.FilterAndOrderBy";
        public const string FilterAndPaging = "Combination.FilterAndPaging";
        public const string SelectAndExpand = "Combination.SelectAndExpand";
        public const string ExpandAndFilter = "Combination.ExpandAndFilter";
        public const string FullPipeline = "Combination.FullPipeline";
        public const string FilterAndExpand = "Combination.FilterAndExpand";
        public const string OrderByAndPaging = "Combination.OrderByAndPaging";
        public const string CountAndPaging = "Combination.CountAndPaging";
        public const string ApplyAndFilter = "Combination.ApplyAndFilter";
        public const string ApplyAndOrderBy = "Combination.ApplyAndOrderBy";
        public const string KitchenSink = "Combination.KitchenSink";
    }
}
