%%%stage 理论数据xx1 误差实际数据xx2 校正后数据xx3 
%%%求模拟数据的校准系数
clc;clearvars;close all;


data_rows=10;
data_cols=10;
fid1 = fopen('C:\Users\Administrator\Desktop\rawdata\src.csv');
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
fid2= fopen('C:\Users\Administrator\Desktop\rawdata\dst.csv');
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

xx4=xx1+(xx1(1,2)-xx1(1,1))*8;

%%% 校准系数求解  角度校准系数
lengthx=data_rows;lengthy=data_cols;

xx3=xx2-xx1;
yy3=yy2-yy1;
xx5=xx3;
yy5=yy3;

figure(1)
surf(xx3);
figure(2)
surf(yy3);

for i = 1:lengthx
    bad_point_index=[];
    good_point_index=[];
    good_point=[];
    for j = 1:lengthy
        if xx3(i,j)==0 %剔除这个元素
            bad_point_index(end+1)=j;
        else
            good_point_index(end+1)=j;
            good_point(end+1)=xx3(i,j);
        end
    end
    if length(bad_point_index)==0
        continue;
    else
        [fitresult1, gof1] = createFit(good_point_index, good_point);%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
        k1=fitresult1.p1;
        b1=fitresult1.p2;
        for k=1:length(bad_point_index)
            xx3(i,bad_point_index(k))=k1*bad_point_index(k)+b1;
        end
    end
end

for i = 1:lengthx
    bad_point_index=[];
    good_point_index=[];
    good_point=[];
    for j = 1:lengthy
        if yy3(i,j)==0 %剔除这个元素
            bad_point_index(end+1)=j;
        else
            good_point_index(end+1)=j;
            good_point(end+1)=yy3(i,j);
        end
    end
    if length(bad_point_index)==0
        continue;
    else
        [fitresult1, gof1] = createFit(good_point_index, good_point);%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
        k1=fitresult1.p1;                                                   
        b1=fitresult1.p2;
        for k=1:length(bad_point_index)
            yy3(i,bad_point_index(k))=k1*bad_point_index(k)+b1;
        end
    end
end

% for i= 1:lengthx
%     for j=1:lengthy
%         if xx1(i,j)==xx4(i,j)
%             
%             

%         