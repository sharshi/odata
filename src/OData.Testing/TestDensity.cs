namespace OData.Testing;

/// <summary>
/// Controls how many test cases are generated per subcategory per entity.
/// Use this to balance thoroughness vs test execution time.
/// </summary>
public enum TestDensity
{
    /// <summary>
    /// ~1 test per subcategory per entity. Quick smoke test.
    /// </summary>
    Minimal = 1,

    /// <summary>
    /// ~3 tests per subcategory per entity. Good balance of coverage and speed.
    /// </summary>
    Balanced = 3,

    /// <summary>
    /// ~5 tests per subcategory per entity. Thorough coverage.
    /// </summary>
    Thorough = 5,

    /// <summary>
    /// All permutations. Hundreds of tests per entity. Full coverage.
    /// </summary>
    Comprehensive = 0
}
