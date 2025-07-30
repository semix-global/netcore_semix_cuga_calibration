%%%stage 理论数据xx1 误差实际数据xx2 校正后数据xx3 
%%%求模拟数据的校准系数
clc;clearvars;close all;


data_rows=18;
data_cols=18;
fid1 = fopen('C:\Users\DELL\Desktop\rawdataB1\src.csv');
data_rows1=data_rows;
data_cols1=data_cols;
data1= textscan(fid1, repmat('%s',[1,data_cols1]),data_rows1,'delimiter', ',');
fclose(fid1);

x_array1=zeros(data_rows1,data_cols1);
y_array1=zeros(data_rows1,data_cols1);
for i=1:data_cols1
     temp11=data1{i};
    for j=1:data_rows1
        temp12=temp11{j};
        index1=find(temp12=='^');
        
        x_temp1=str2double(temp12(1:index1-1));
        y_temp1=str2double(temp12(index1+1:end));
        
        x_array1(j,i)= x_temp1;
        y_array1(j,i)= y_temp1;
    end
end

%%实际值
fid2= fopen('C:\Users\DELL\Desktop\rawdataB1\dst.csv');
data_rows2=data_rows;
data_cols2=data_cols;
data2= textscan(fid2, repmat('%s',[1,data_cols2]),data_rows2,'delimiter', ',');
fclose(fid2);

x_array2=zeros(data_rows2,data_cols2);
y_array2=zeros(data_rows2,data_cols2);
for i=1:data_cols2
     temp21=data2{i};
   
        for j=1:data_rows2
            temp22=temp21{j};
            index2=find(temp22=='^');
        
            x_temp2=str2double(temp22(1:index2-1));
            y_temp2=str2double(temp22(index2+1:end));
        
            x_array2(j,i)= x_temp2;
            y_array2(j,i)= y_temp2;
        end
end
xx1=x_array1;
yy1=y_array1;
xx2=x_array2;
yy2=y_array2;

%%% 校准系数求解  角度校准系数
lengthx=data_rows;lengthy=data_cols;
xx3=xx2-xx1;
yy3=yy2-yy1;



for i = 1:lengthx
    ave = mean(xx3(i,:));%mean 求解平均值
    u = std(xx3(i,:));%求解标准差
    a=[];
    b=[];
    for j = 1:lengthy
        if(abs(xx3(i,j)-ave)>3*u)%不符合 3σ准则 ， 剔除这个元素
            a(end+1)=j;
        else
            b(end+1)=xx3(i,j);
        end
    end
    if length(a)==0
        continue;
    else
        for k=1:length(a)
            ave1=mean(b);
            xx3(i,a(k))=ave1;
        end
    end
end
for i = 1:lengthx
    ave = mean(yy3(i,:));%mean 求解平均值
    u = std(yy3(i,:));%求解标准差
    a=[];
    b=[];
    for j = 1:lengthy
        if(abs(yy3(i,j)-ave)>3*u)%不符合 3σ准则 ， 剔除这个元素
            a(end+1)=j;
            disp(a)
        else
            b(end+1)=yy3(i,j);
        end
    end
    if length(a)==0
        continue;
    else
        for k=1:length(a)
            ave1=mean(b);
            yy3(i,a(k))=ave1;
        end
    end
end


xx2=xx1+xx3;
yy2=yy1+yy3;
corrected_error_x = xx2-xx1;
corrected_error_y = yy2-yy1;
%figure(12)
%surf(corrected_error_x);
%figure(13)
%surf(corrected_error_y);
%figure(14)
%quiver(xx1,yy1,corrected_error_x,corrected_error_y,'r','AutoScaleFactor',1,'ShowArrowHead','on');title('模拟误差矢量图');
for i=2:lengthx
    for j=2:lengthy
        corrected_error_x(i,j)=corrected_error_x(i,j)-corrected_error_x(1,1);
        corrected_error_y(i,j)=corrected_error_y(i,j)-corrected_error_y(1,1);
    end
end
figure(12)
surf(corrected_error_x);
figure(13)
surf(corrected_error_y);


for i=1:lengthx
  [fitresult1, gof1] = createFit(xx1(i,:), yy1(i,:));%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k1=fitresult1.p1;
  b1=fitresult1.p2;
 % k1_array(i)=k1;
 % b1_array(i)=b1;
  [fitresult2, gof2] = createFit(xx2(i,:), yy2(i,:));%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k2=fitresult2.p1;
  b2=fitresult2.p2;
  %k2_array(i)=k2;
  theta(i)=atan(((k2-k1)/(1+k1*k2)))*180/pi;  %%%每行的夹角
end

mean_theta=mean(theta);
%%%角度校准
sita=mean_theta*pi/180;
Tj=[cos(sita) -1*sin(sita) 0
    sin(sita) cos(sita) 0
    0 0 1];
for i=1:1:lengthx
    for j=1:1:lengthy
        temp= Tj*[xx1(i,j) yy1(i,j) 1]';
        xx1(i,j)=temp(1);
        yy1(i,j)=temp(2);
    end
end

for i=1:lengthx
  [fitresult1, gof1] = createFit(xx1(:,i), yy1(:,i));%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k1=fitresult1.p1;
  b1=fitresult1.p2;
 % k1_array(i)=k1;
 % b1_array(i)=b1;
  [fitresult2, gof2] = createFit(xx2(:,i), yy2(:,i));%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k2=fitresult2.p1;
  b2=fitresult2.p2;

  theta(i)=atan(((k2-k1)/(1+k1*k2)))*180/pi;  %%%每列的夹角
end

mean_theta1=mean(theta)*pi/180;

 
for i=1:lengthx
    for j=1:lengthy
        map(i,j)=-(yy1(i,j)-yy1(1,j))*tan(mean_theta1);
    end
end
figure(1);
surf(map)

corrected_error_x=zeros(data_rows2,data_cols2);
corrected_error_y=zeros(data_rows2,data_cols2);
xx1=xx1+map;
for i=1:lengthx
    for j=1:lengthy
        corrected_error_x(i,j)=xx2(i,j)-xx1(i,j);
        corrected_error_y(i,j)=yy2(i,j)-yy1(i,j);
    end
end

figure(16)
surf(corrected_error_y)
%%%缩放校准系数
for i=1:lengthx
  [fitresult3, gof3] = createFit(xx1(i,:), xx2(i,:));%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k3(i)=fitresult3.p1;
  b3=fitresult3.p2;
end
mean_kpx=mean(k3);


for i=1:lengthy
  [fitresult4, gof4] = createFit(yy1(:,i), yy2(:,i));%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k4(i)=fitresult4.p1;
  b4=fitresult4.p2;
end
mean_kpy=mean(k4);



% for i=1:lengthy
%   [fitresult2, gof2] = createFit(yy1(i,:), yy2(i,:));%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
%   k2(i)=fitresult2.p1;
%   b2=fitresult2.p2;
% end
% mean_kpy1=mean(k2);
%%缩放校准
Ts=[mean_kpx 0 0
    0 mean_kpy 0
    0 0 1];
for i=1:1:lengthx
    for j=1:1:lengthy
        temp= Ts*[xx1(i,j) yy1(i,j) 1]';
        xx1(i,j)=temp(1);
        yy1(i,j)=temp(2);
    end
end

corrected_error_x=xx2-xx1;
corrected_error_y=yy2-yy1;


%平移坐标修正
tx=sum(corrected_error_x(:))/(lengthx*lengthy);
ty=sum(corrected_error_y(:))/(lengthx*lengthy);
corrected_error_x=corrected_error_x-tx;
corrected_error_y=corrected_error_y-ty;

%figure(12)
%surf(corrected_error_x);
%figure(13)
%surf(corrected_error_y);
%figure(14)
%quiver(xx1,yy1,corrected_error_x,corrected_error_y,'r','AutoScaleFactor',1,'ShowArrowHead','on');title('模拟误差矢量图');


for i=1:lengthx
    p = polyfit(1:lengthy,corrected_error_x(i,:),5);
   % poly2str(p,'x');
    corrected_error_x(i,:) = polyval(p,1:lengthy);
end
for i=1:lengthx
    p = polyfit(1:lengthy,corrected_error_y(i,:),5);
    corrected_error_y(i,:) = polyval(p,1:lengthy);
end


figure(4);
surf(corrected_error_x)
figure(7);
surf(corrected_error_y)
corrected_error_x=corrected_error_x+map;
figure(10);
surf(corrected_error_x)
a=corrected_error_x;
figure(8);
quiver(xx1,yy1,corrected_error_x,corrected_error_y,'r','AutoScaleFactor',1,'ShowArrowHead','on');title('模拟误差矢量图');
fid = fopen('C:\Users\DELL\Desktop\rawdataB1\x.txt','w');
fprintf(fid,[repmat('%5.9f\t', 1, size(a,2)), '\n'], a');     
fclose(fid);
b=corrected_error_y;
fid = fopen('C:\Users\DELL\Desktop\rawdataB1\y.txt','w');
fprintf(fid,[repmat('%5.9f\t', 1, size(b,2)), '\n'], b');     
fclose(fid);
