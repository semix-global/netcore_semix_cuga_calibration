%%%%Savitzky-Golay滤波法 三次九点
%%%p邻近数据的加权系数 m=8九点去噪效果不够，噪声峰多m=9效果和小波类似，相比小波计算量更少 m=9
function y1nd=Savitzky_Golay(y1n)
%%%%2000之前的谱线
LGT=40;y1n=y1n';
y1n1=y1n(1:LGT);
m=5;j=-1*m:1:m;
y1n1=[y1n(1)*ones(m,1) ;y1n1; y1n(LGT)*ones(m,1)];  %%%共2799+2*m
length=LGT+2*m;
p=3/(2*m+3)/(2*m+1)/(2*m-1)*(3*m^2+3*m-1-5*j.*j);

for i1=1:m
    y1n1(i1)=y1n1(i1);
end
for i1=m+1:length-m-1
  y1nd1(i1)=sum(y1n1(i1-m:i1+m)'.*p);
end
for i1=length-m:length
    y1nd1(i1)=y1n1(i1);
end
y1nd(1:LGT)=y1nd1(m+1:m+LGT);
%%%%%1999-4096之后的谱线 m=21
% y1n2=y1n(1999:4096);
% m=31;j=-1*m:1:m;
% y1n2=[y1n(1999)*ones(m,1) ;y1n2; y1n(4096)*ones(m,1)];
% p=3/(2*m+3)/(2*m+1)/(2*m-1)*(3*m^2+3*m-1-5*j.*j);
% length=2098+2*m;
% for i1=1:m
%     y1n2(i1)=y1n2(i1);
% end
% for i1=m+1:length-m-1
%   y1nd2(i1)=sum(y1n2(i1-m:i1+m)'.*p);
% end
% for i1=length-m:length
%     y1nd2(i1)=y1n2(i1);
% end
% y1nd(1999:4096)=y1nd2(m+1:m+2098);






