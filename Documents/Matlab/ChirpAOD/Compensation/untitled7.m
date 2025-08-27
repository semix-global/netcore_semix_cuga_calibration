t = readtable("C:\Users\DELL\Downloads\新建 XLSX 工作表.xlsx");
data = table2array(t); 
for i = 1:size(data,1)
    x = data(i,1)+1; % MATLAB索引从1开始
    y = data(i,2)+1;
    Z(x,y) = data(i,3);
end
poltData = Z';
bar3(Z'); % 注意需要转置
xlabel('X');
ylabel('Y');
zlabel('Z');