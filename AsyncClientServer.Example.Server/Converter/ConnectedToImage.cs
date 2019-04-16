using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace AsyncClientServer.Example.Server.Converter
{
	[ValueConversion(typeof(Boolean), typeof(string))]
    public class ConnectedToImage: IValueConverter
    {
	    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	    {
		    try
		    {
			    Boolean connected = (bool) value;

			    if (connected)
				    return @"../Images/Icons/link.png";
			    


				return @"../Images/Icons/unlinked.png";

		    }
		    catch (Exception ex)
		    {
			    throw new Exception(ex.Message, ex);
		    }
	    }

	    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	    {
		    throw new NotImplementedException();
	    }
    }
}
