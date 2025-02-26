using System.Collections.Generic;
using ContextRunner.State.Sanitizers;
using Xunit;

namespace ContextRunner.Tests.BaseTests
{
    public class ContextStateTests
    {
        [Fact]
        public void ToShallowObjectTruncatesAtMaxDepth()
        {
            var testObject = new
            {
                Level1 = new
                {
                    Level2 = new[]
                    {
                        new
                        {
                            Level3 = "Testing!"
                        }
                    }
                }
            };

            var result = testObject.ToShallowObject();

            Assert.Equal("~truncated~", result.Level1.Level2);
        }

        [Fact]
        public void KeyBasedSanitizerTruncatesAtMaxDepth()
        {
            var testObject = new
            {
                Level1 = new
                {
                    Level2 = new
                    {
                        Level3 = "Testing!"
                    }
                }
            };

            var sanitizer = new KeyBasedSanitizer(["sanitizeMe"], 2);
            
            var result = sanitizer.Sanitize(new KeyValuePair<string, object?>("test", testObject));

            Assert.Equal("~truncated~", result?.Level1.Level2);
        }
    }
}