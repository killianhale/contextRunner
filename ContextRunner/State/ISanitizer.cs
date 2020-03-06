using System;
using System.Collections.Generic;

namespace ContextRunner.State
{
    public interface ISanitizer
    {
        object Sanitize(KeyValuePair<string, object> contextParam);
    }
}
