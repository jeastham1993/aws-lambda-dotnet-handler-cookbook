using SharedKernel.Features;
using FakeItEasy;

namespace Stocks.Tests.Shared;

public static class FeatureFlagMocks
{
    private static IFeatureFlags _mockFeatureFlags;
    private static IFeatureFlags _reduceMockFeatureFlags;
    private static IFeatureFlags _increaseMockFeatureFlags;
    
    public static IFeatureFlags Default
    {
        get
        {
            if (_mockFeatureFlags != null)
            {
                return _mockFeatureFlags;
            }
            
            _mockFeatureFlags = A.Fake<IFeatureFlags>();
            A.CallTo(
                    () => _mockFeatureFlags.Evaluate(
                        A<string>._,
                        A<Dictionary<string, object>>._,
                        A<object>._))
                .Returns("False");

            A.CallTo(
                    () => _mockFeatureFlags.Evaluate(
                        "ten_percent_share_increase",
                        A<Dictionary<string, object>>._,
                        A<object>._))
                .Returns("False");

            return _mockFeatureFlags;
        }
    }
    
    public static IFeatureFlags ReduceStockPriceEnabled
    {
        get
        {
            if (_reduceMockFeatureFlags != null)
            {
                return _reduceMockFeatureFlags;
            }
            
            _reduceMockFeatureFlags = A.Fake<IFeatureFlags>();
            A.CallTo(
                    () => _reduceMockFeatureFlags.Evaluate(
                        A<string>._,
                        A<Dictionary<string, object>>._,
                        A<object>._))
                .Returns("True");

            A.CallTo(
                    () => _reduceMockFeatureFlags.Evaluate(
                        "ten_percent_share_increase",
                        A<Dictionary<string, object>>._,
                        A<object>._))
                .Returns("False");

            return _reduceMockFeatureFlags;
        }
    }
    
    public static IFeatureFlags IncreaseStockPriceEnabled
    {
        get
        {
            if (_increaseMockFeatureFlags != null)
            {
                return _increaseMockFeatureFlags;
            }

            _increaseMockFeatureFlags = A.Fake<IFeatureFlags>();
            A.CallTo(
                    () => _increaseMockFeatureFlags.Evaluate(
                        A<string>._,
                        A<Dictionary<string, object>>._,
                        A<object>._))
                .Returns("False");

            A.CallTo(
                    () => _increaseMockFeatureFlags.Evaluate(
                        "ten_percent_share_increase",
                        A<Dictionary<string, object>>._,
                        A<object>._))
                .Returns("True");

            return _increaseMockFeatureFlags;
        }
    }
}