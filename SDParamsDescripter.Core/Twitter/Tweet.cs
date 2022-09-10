using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDParamsDescripter.Core.Twitter;
public record Tweet(string Text, string ImagePath, string ImageAltText, bool ResizeImageWhenTooLarge);
