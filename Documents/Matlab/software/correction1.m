%%%stage 理论数据xx1 误差实际数据xx2 校正后数据xx3 
%%%求模拟数据的校准系数
clc;clearvars;close all;

%%%理论数据xx1
%load( 'new_x_array1.mat');
%load( 'new_y_array1.mat');
%load( 'new_x_array2.mat');
%load( 'new_y_array2.mat');

data_rows=18;
data_cols=18;
fid1 = fopen('C:\Users\DELL\Desktop\rawdata\src.csv');
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
fid2= fopen('C:\Users\DELL\Desktop\rawdata\dst.csv');
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

lengthx=18;lengthy=18;
%%% 校准系数求解  角度校准系数
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
  theta(i)=atan(((k2-k1)/(1+k1*k2)))*180/pi;  %%%每列的夹角
end

mean_theta=mean(theta);
%%%角度校准
sita=-mean_theta*pi/180;
Tj=[cos(sita) sin(sita) 0
    -1*sin(sita) cos(sita) 0
    0 0 1];
for i=1:1:lengthx
    for j=1:1:lengthy
        temp= Tj*[xx1(i,j) yy1(i,j) 1]';
        xx1(i,j)=temp(1);
        yy1(i,j)=temp(2);
    end
end
corrected_error_x=zeros(data_rows2,data_cols2);
corrected_error_y=zeros(data_rows2,data_cols2);
for i=1:lengthx
    for j=1:lengthy
        corrected_error_x(i,j)=xx2(i,j)-xx1(i,j);
        corrected_error_y(i,j)=yy2(i,j)-yy1(i,j);
    end
end
figure(2);
surf(corrected_error_x)
figure(3);
surf(corrected_error_y)
for i=1:lengthx
  [fitresult1, gof1] = createFit(xx1(:,i), yy1(:,i));%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k1=fitresult1.p1;
  b1=fitresult1.p2;
 % k1_array(i)=k1;
 % b1_array(i)=b1;
  [fitresult2, gof2] = createFit(xx2(:,i), yy2(:,i));%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
  k2=fitresult2.p1;
  b2=fitresult2.p2;
  %k2_array(i)=k2;
  theta(i)=atan(((k2-k1)/(1+k1*k2)))*180/pi;  %%%每列的夹角
end
mean_theta1=mean(theta);
mlm=tan(mean_theta1*pi/180);
 
for i=1:lengthx
    for j=1:lengthy
        map(i,j)=(yy1(i,j)-yy1(1,j))*tan(mean_theta1*pi/180);
    end
end
figure(1);
surf(map)
b= corrected_error_x+map;
figure(4);
surf(b)
a=map;
fid = fopen('C:\Users\DELL\Desktop\rawdata\x1.txt','w');
fprintf(fid,[repmat('%5.9f\t', 1, size(a,2)), '\n'], a');     
fclose(fid);
disp(a)