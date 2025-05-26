function [xArray, yArray] = ReadChuckCsvFile1(file)

    % 打开文件
    fid = fopen(file);

    % 检查文件是否成功打开
    if fid == -1
        error('Failed to open file.');
    end

    data_rows1=7;
    data_cols1=7;
    data1= textscan(fid, repmat('%s',[1,data_cols1]),data_rows1,'delimiter', ',');
    fclose(fid);
    
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
    
    xArray = x_array1;
    yArray = y_array1;

end
