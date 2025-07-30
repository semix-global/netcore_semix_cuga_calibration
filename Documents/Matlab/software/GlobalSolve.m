
function [x,fval,exitFlag]=GlobalSolve(@myfun,n,lb,ub)
x0 = particleswarm(@(x)sum(myfun(x).^2),5,lb,ub); %利用 particleswarm 得到一个搜索初值
opts = optimoptions(@fmincon,'Algorithm','interior-point','Display','off');
problem  = createOptimProblem('fmincon','x0',x0,'objective',@(x)0,'lb',lb,'ub',ub,'nonlcon',@(x)constrain(x,@myfun));
gs = GlobalSearch;
[x,~,exitflag] = gs.run(problem) %全局搜索
