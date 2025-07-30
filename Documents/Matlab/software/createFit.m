function [fitresult, gof] = createFit(xx1, yy1)
    [xData, yData] = prepareCurveData( xx1, yy1 );
    ft = fittype( 'poly1' );
    [fitresult, gof] = fit( xData, yData, ft );
end