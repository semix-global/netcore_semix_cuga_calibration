% fmincon 的非线性约束
function [c ceq] = constrain(x,f)
ceq = f(x);
c =[];