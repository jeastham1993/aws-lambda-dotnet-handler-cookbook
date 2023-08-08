using Moq;
using SharedKernel.Features;

namespace Stocks.Tests.Shared;

public static class FeatureFlagMocks
{
    private static Mock<IFeatureFlags> _mockFeatureFlags;
    private static Mock<IFeatureFlags> _reduceMockFeatureFlags;
    
    public static IFeatureFlags Default
    {
        get
        {
            if (_mockFeatureFlags != null)
            {
                return _mockFeatureFlags.Object;
            }
            
            _mockFeatureFlags = new Mock<IFeatureFlags>();
            _mockFeatureFlags.Setup(
                    p => p.Evaluate(
                        It.IsAny<string>(),
                        It.IsAny<Dictionary<string, object>>(),
                        It.IsAny<object>()))
                .Returns("False");
            _mockFeatureFlags.Setup(
                    p => p.Evaluate(
                        "ten_percent_share_increase",
                        It.IsAny<Dictionary<string, object>>(),
                        It.IsAny<object>()))
                .Returns("False");

            return _mockFeatureFlags.Object;
        }
    }
    
    public static IFeatureFlags ReduceStockPriceEnabled
    {
        get
        {
            if (_reduceMockFeatureFlags != null)
            {
                return _reduceMockFeatureFlags.Object;
            }
            
            _reduceMockFeatureFlags = new Mock<IFeatureFlags>();
            _reduceMockFeatureFlags.Setup(
                    p => p.Evaluate(
                        It.IsAny<string>(),
                        It.IsAny<Dictionary<string, object>>(),
                        It.IsAny<object>()))
                .Returns("True");
            _reduceMockFeatureFlags.Setup(
                    p => p.Evaluate(
                        "ten_percent_share_increase",
                        It.IsAny<Dictionary<string, object>>(),
                        It.IsAny<object>()))
                .Returns("False");

            return _reduceMockFeatureFlags.Object;
        }
    }
}