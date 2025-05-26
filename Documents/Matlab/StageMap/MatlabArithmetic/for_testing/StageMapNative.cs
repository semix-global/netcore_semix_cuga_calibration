/*
* MATLAB Compiler: 23.2 (R2023b)
* Date: Tue Jun 25 14:08:20 2024
* Arguments:
* "-B""macro_default""-W""dotnet:MatlabArithmetic,StageMap,4.0,private,version=1.0""-T""li
* nk:lib""-d""C:\Users\DELL\RiderProjects\semix\cuga2.0\Calibration\Calibration.Wpf\Matlab
* \StageMap\MatlabArithmetic\for_testing""-v""class{StageMap:C:\Users\DELL\RiderProjects\s
* emix\cuga2.0\Calibration\Calibration.Wpf\Matlab\StageMap\CalculateChuckStageMapError.m,C
* :\Users\DELL\RiderProjects\semix\cuga2.0\Calibration\Calibration.Wpf\Matlab\StageMap\Cre
* ateFit.m}"
*/
using System;
using System.Reflection;
using System.IO;
using MathWorks.MATLAB.NET.Arrays;
using MathWorks.MATLAB.NET.Utility;

#if SHARED
[assembly: System.Reflection.AssemblyKeyFile(@"")]
#endif

namespace MatlabArithmeticNative
{

  /// <summary>
  /// The StageMap class provides a CLS compliant, Object (native) interface to the
  /// MATLAB functions contained in the files:
  /// <newpara></newpara>
  /// C:\Users\DELL\RiderProjects\semix\cuga2.0\Calibration\Calibration.Wpf\Matlab\StageMa
  /// p\CalculateChuckStageMapError.m
  /// <newpara></newpara>
  /// C:\Users\DELL\RiderProjects\semix\cuga2.0\Calibration\Calibration.Wpf\Matlab\StageMa
  /// p\CreateFit.m
  /// </summary>
  /// <remarks>
  /// @Version 1.0
  /// </remarks>
  public class StageMap : IDisposable
  {
    #region Constructors

    /// <summary internal= "true">
    /// The static constructor instantiates and initializes the MATLAB Runtime instance.
    /// </summary>
    static StageMap()
    {
      if (MWMCR.MCRAppInitialized)
      {
        try
        {
          System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();

          string ctfFilePath= assembly.Location;

		  int lastDelimiter = ctfFilePath.LastIndexOf(@"/");

	      if (lastDelimiter == -1)
		  {
		    lastDelimiter = ctfFilePath.LastIndexOf(@"\");
		  }

          ctfFilePath= ctfFilePath.Remove(lastDelimiter, (ctfFilePath.Length - lastDelimiter));

          string ctfFileName = "MatlabArithmetic.ctf";

          Stream embeddedCtfStream = null;

          String[] resourceStrings = assembly.GetManifestResourceNames();

          foreach (String name in resourceStrings)
          {
            if (name.Contains(ctfFileName))
            {
              embeddedCtfStream = assembly.GetManifestResourceStream(name);
              break;
            }
          }
          mcr= new MWMCR("",
                         ctfFilePath, embeddedCtfStream, true);
        }
        catch(Exception ex)
        {
          ex_ = new Exception("MWArray assembly failed to be initialized", ex);
        }
      }
      else
      {
        ex_ = new ApplicationException("MWArray assembly could not be initialized");
      }
    }


    /// <summary>
    /// Constructs a new instance of the StageMap class.
    /// </summary>
    public StageMap()
    {
      if(ex_ != null)
      {
        throw ex_;
      }
    }


    #endregion Constructors

    #region Finalize

    /// <summary internal= "true">
    /// Class destructor called by the CLR garbage collector.
    /// </summary>
    ~StageMap()
    {
      Dispose(false);
    }


    /// <summary>
    /// Frees the native resources associated with this object
    /// </summary>
    public void Dispose()
    {
      Dispose(true);

      GC.SuppressFinalize(this);
    }


    /// <summary internal= "true">
    /// Internal dispose function
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
      if (!disposed)
      {
        disposed= true;

        if (disposing)
        {
          // Free managed resources;
        }

        // Free native resources
      }
    }


    #endregion Finalize

    #region Methods

    /// <summary>
    /// Provides a single output, 0-input Objectinterface to the
    /// CalculateChuckStageMapError MATLAB function.
    /// </summary>
    /// <remarks>
    /// M-Documentation:
    /// 获取行数和列数
    /// </remarks>
    /// <returns>An Object containing the first output argument.</returns>
    ///
    public Object CalculateChuckStageMapError()
    {
      return mcr.EvaluateFunction("CalculateChuckStageMapError", new Object[]{});
    }


    /// <summary>
    /// Provides a single output, 1-input Objectinterface to the
    /// CalculateChuckStageMapError MATLAB function.
    /// </summary>
    /// <remarks>
    /// M-Documentation:
    /// 获取行数和列数
    /// </remarks>
    /// <param name="xx1">Input argument #1</param>
    /// <returns>An Object containing the first output argument.</returns>
    ///
    public Object CalculateChuckStageMapError(Object xx1)
    {
      return mcr.EvaluateFunction("CalculateChuckStageMapError", xx1);
    }


    /// <summary>
    /// Provides a single output, 2-input Objectinterface to the
    /// CalculateChuckStageMapError MATLAB function.
    /// </summary>
    /// <remarks>
    /// M-Documentation:
    /// 获取行数和列数
    /// </remarks>
    /// <param name="xx1">Input argument #1</param>
    /// <param name="yy1">Input argument #2</param>
    /// <returns>An Object containing the first output argument.</returns>
    ///
    public Object CalculateChuckStageMapError(Object xx1, Object yy1)
    {
      return mcr.EvaluateFunction("CalculateChuckStageMapError", xx1, yy1);
    }


    /// <summary>
    /// Provides a single output, 3-input Objectinterface to the
    /// CalculateChuckStageMapError MATLAB function.
    /// </summary>
    /// <remarks>
    /// M-Documentation:
    /// 获取行数和列数
    /// </remarks>
    /// <param name="xx1">Input argument #1</param>
    /// <param name="yy1">Input argument #2</param>
    /// <param name="xx2">Input argument #3</param>
    /// <returns>An Object containing the first output argument.</returns>
    ///
    public Object CalculateChuckStageMapError(Object xx1, Object yy1, Object xx2)
    {
      return mcr.EvaluateFunction("CalculateChuckStageMapError", xx1, yy1, xx2);
    }


    /// <summary>
    /// Provides a single output, 4-input Objectinterface to the
    /// CalculateChuckStageMapError MATLAB function.
    /// </summary>
    /// <remarks>
    /// M-Documentation:
    /// 获取行数和列数
    /// </remarks>
    /// <param name="xx1">Input argument #1</param>
    /// <param name="yy1">Input argument #2</param>
    /// <param name="xx2">Input argument #3</param>
    /// <param name="yy2">Input argument #4</param>
    /// <returns>An Object containing the first output argument.</returns>
    ///
    public Object CalculateChuckStageMapError(Object xx1, Object yy1, Object xx2, Object 
                                        yy2)
    {
      return mcr.EvaluateFunction("CalculateChuckStageMapError", xx1, yy1, xx2, yy2);
    }


    /// <summary>
    /// Provides the standard 0-input Object interface to the CalculateChuckStageMapError
    /// MATLAB function.
    /// </summary>
    /// <remarks>
    /// M-Documentation:
    /// 获取行数和列数
    /// </remarks>
    /// <param name="numArgsOut">The number of output arguments to return.</param>
    /// <returns>An Array of length "numArgsOut" containing the output
    /// arguments.</returns>
    ///
    public Object[] CalculateChuckStageMapError(int numArgsOut)
    {
      return mcr.EvaluateFunction(numArgsOut, "CalculateChuckStageMapError", new Object[]{});
    }


    /// <summary>
    /// Provides the standard 1-input Object interface to the CalculateChuckStageMapError
    /// MATLAB function.
    /// </summary>
    /// <remarks>
    /// M-Documentation:
    /// 获取行数和列数
    /// </remarks>
    /// <param name="numArgsOut">The number of output arguments to return.</param>
    /// <param name="xx1">Input argument #1</param>
    /// <returns>An Array of length "numArgsOut" containing the output
    /// arguments.</returns>
    ///
    public Object[] CalculateChuckStageMapError(int numArgsOut, Object xx1)
    {
      return mcr.EvaluateFunction(numArgsOut, "CalculateChuckStageMapError", xx1);
    }


    /// <summary>
    /// Provides the standard 2-input Object interface to the CalculateChuckStageMapError
    /// MATLAB function.
    /// </summary>
    /// <remarks>
    /// M-Documentation:
    /// 获取行数和列数
    /// </remarks>
    /// <param name="numArgsOut">The number of output arguments to return.</param>
    /// <param name="xx1">Input argument #1</param>
    /// <param name="yy1">Input argument #2</param>
    /// <returns>An Array of length "numArgsOut" containing the output
    /// arguments.</returns>
    ///
    public Object[] CalculateChuckStageMapError(int numArgsOut, Object xx1, Object yy1)
    {
      return mcr.EvaluateFunction(numArgsOut, "CalculateChuckStageMapError", xx1, yy1);
    }


    /// <summary>
    /// Provides the standard 3-input Object interface to the CalculateChuckStageMapError
    /// MATLAB function.
    /// </summary>
    /// <remarks>
    /// M-Documentation:
    /// 获取行数和列数
    /// </remarks>
    /// <param name="numArgsOut">The number of output arguments to return.</param>
    /// <param name="xx1">Input argument #1</param>
    /// <param name="yy1">Input argument #2</param>
    /// <param name="xx2">Input argument #3</param>
    /// <returns>An Array of length "numArgsOut" containing the output
    /// arguments.</returns>
    ///
    public Object[] CalculateChuckStageMapError(int numArgsOut, Object xx1, Object yy1, 
                                          Object xx2)
    {
      return mcr.EvaluateFunction(numArgsOut, "CalculateChuckStageMapError", xx1, yy1, xx2);
    }


    /// <summary>
    /// Provides the standard 4-input Object interface to the CalculateChuckStageMapError
    /// MATLAB function.
    /// </summary>
    /// <remarks>
    /// M-Documentation:
    /// 获取行数和列数
    /// </remarks>
    /// <param name="numArgsOut">The number of output arguments to return.</param>
    /// <param name="xx1">Input argument #1</param>
    /// <param name="yy1">Input argument #2</param>
    /// <param name="xx2">Input argument #3</param>
    /// <param name="yy2">Input argument #4</param>
    /// <returns>An Array of length "numArgsOut" containing the output
    /// arguments.</returns>
    ///
    public Object[] CalculateChuckStageMapError(int numArgsOut, Object xx1, Object yy1, 
                                          Object xx2, Object yy2)
    {
      return mcr.EvaluateFunction(numArgsOut, "CalculateChuckStageMapError", xx1, yy1, xx2, yy2);
    }


    /// <summary>
    /// Provides an interface for the CalculateChuckStageMapError function in which the
    /// input and output
    /// arguments are specified as an array of Objects.
    /// </summary>
    /// <remarks>
    /// This method will allocate and return by reference the output argument
    /// array.<newpara></newpara>
    /// M-Documentation:
    /// 获取行数和列数
    /// </remarks>
    /// <param name="numArgsOut">The number of output arguments to return</param>
    /// <param name= "argsOut">Array of Object output arguments</param>
    /// <param name= "argsIn">Array of Object input arguments</param>
    /// <param name= "varArgsIn">Array of Object representing variable input
    /// arguments</param>
    ///
    [MATLABSignature("CalculateChuckStageMapError", 4, 2, 0)]
    protected void CalculateChuckStageMapError(int numArgsOut, ref Object[] argsOut, Object[] argsIn, params Object[] varArgsIn)
    {
        mcr.EvaluateFunctionForTypeSafeCall("CalculateChuckStageMapError", numArgsOut, ref argsOut, argsIn, varArgsIn);
    }
    /// <summary>
    /// Provides a single output, 0-input Objectinterface to the CreateFit MATLAB
    /// function.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <returns>An Object containing the first output argument.</returns>
    ///
    public Object CreateFit()
    {
      return mcr.EvaluateFunction("CreateFit", new Object[]{});
    }


    /// <summary>
    /// Provides a single output, 1-input Objectinterface to the CreateFit MATLAB
    /// function.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="xx1">Input argument #1</param>
    /// <returns>An Object containing the first output argument.</returns>
    ///
    public Object CreateFit(Object xx1)
    {
      return mcr.EvaluateFunction("CreateFit", xx1);
    }


    /// <summary>
    /// Provides a single output, 2-input Objectinterface to the CreateFit MATLAB
    /// function.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="xx1">Input argument #1</param>
    /// <param name="yy1">Input argument #2</param>
    /// <returns>An Object containing the first output argument.</returns>
    ///
    public Object CreateFit(Object xx1, Object yy1)
    {
      return mcr.EvaluateFunction("CreateFit", xx1, yy1);
    }


    /// <summary>
    /// Provides the standard 0-input Object interface to the CreateFit MATLAB function.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="numArgsOut">The number of output arguments to return.</param>
    /// <returns>An Array of length "numArgsOut" containing the output
    /// arguments.</returns>
    ///
    public Object[] CreateFit(int numArgsOut)
    {
      return mcr.EvaluateFunction(numArgsOut, "CreateFit", new Object[]{});
    }


    /// <summary>
    /// Provides the standard 1-input Object interface to the CreateFit MATLAB function.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="numArgsOut">The number of output arguments to return.</param>
    /// <param name="xx1">Input argument #1</param>
    /// <returns>An Array of length "numArgsOut" containing the output
    /// arguments.</returns>
    ///
    public Object[] CreateFit(int numArgsOut, Object xx1)
    {
      return mcr.EvaluateFunction(numArgsOut, "CreateFit", xx1);
    }


    /// <summary>
    /// Provides the standard 2-input Object interface to the CreateFit MATLAB function.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="numArgsOut">The number of output arguments to return.</param>
    /// <param name="xx1">Input argument #1</param>
    /// <param name="yy1">Input argument #2</param>
    /// <returns>An Array of length "numArgsOut" containing the output
    /// arguments.</returns>
    ///
    public Object[] CreateFit(int numArgsOut, Object xx1, Object yy1)
    {
      return mcr.EvaluateFunction(numArgsOut, "CreateFit", xx1, yy1);
    }


    /// <summary>
    /// Provides an interface for the CreateFit function in which the input and output
    /// arguments are specified as an array of Objects.
    /// </summary>
    /// <remarks>
    /// This method will allocate and return by reference the output argument
    /// array.<newpara></newpara>
    /// </remarks>
    /// <param name="numArgsOut">The number of output arguments to return</param>
    /// <param name= "argsOut">Array of Object output arguments</param>
    /// <param name= "argsIn">Array of Object input arguments</param>
    /// <param name= "varArgsIn">Array of Object representing variable input
    /// arguments</param>
    ///
    [MATLABSignature("CreateFit", 2, 2, 0)]
    protected void CreateFit(int numArgsOut, ref Object[] argsOut, Object[] argsIn, params Object[] varArgsIn)
    {
        mcr.EvaluateFunctionForTypeSafeCall("CreateFit", numArgsOut, ref argsOut, argsIn, varArgsIn);
    }

    /// <summary>
    /// This method will cause a MATLAB figure window to behave as a modal dialog box.
    /// The method will not return until all the figure windows associated with this
    /// component have been closed.
    /// </summary>
    /// <remarks>
    /// An application should only call this method when required to keep the
    /// MATLAB figure window from disappearing.  Other techniques, such as calling
    /// Console.ReadLine() from the application should be considered where
    /// possible.</remarks>
    ///
    public void WaitForFiguresToDie()
    {
      mcr.WaitForFiguresToDie();
    }



    #endregion Methods

    #region Class Members

    private static MWMCR mcr= null;

    private static Exception ex_= null;

    private bool disposed= false;

    #endregion Class Members
  }
}
