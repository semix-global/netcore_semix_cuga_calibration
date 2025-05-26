function [corrected_error_x, corrected_error_y] = CalculateChuckStageMapError(xx1, yy1, xx2, yy2) %校准系数求解  角度校准系数

    %获取行数和列数
    lengthx = size(xx1, 1);
    lengthy = size(xx1, 2);

    xx3 = xx2 - xx1;
    yy3 = yy2 - yy1;

    for i = 1:lengthx
        ave = mean(xx3(i, :)); %mean 求解平均值
        u = std(xx3(i, :)); %求解标准差
        a = [];
        b = [];

        for j = 1:lengthy

            if (abs(xx3(i, j) - ave) > 3 * u) %不符合 3σ准则 ， 剔除这个元素
                a(end + 1) = j;
            else
                b(end + 1) = xx3(i, j);
            end

        end

        if length(a) == 0
            continue;
        else

            for k = 1:length(a)
                ave1 = mean(b);
                xx3(i, a(k)) = ave1;
            end

        end

    end

    for i = 1:lengthx
        ave = mean(yy3(i, :)); %mean 求解平均值
        u = std(yy3(i, :)); %求解标准差
        a = [];
        b = [];

        for j = 1:lengthy

            if (abs(yy3(i, j) - ave) > 3 * u) %不符合 3σ准则 ， 剔除这个元素
                a(end + 1) = j;
                disp(a)
            else
                b(end + 1) = yy3(i, j);
            end

        end

        if length(a) == 0
            continue;
        else

            for k = 1:length(a)
                ave1 = mean(b);
                yy3(i, a(k)) = ave1;
            end

        end

    end

    xx2 = xx1 + xx3;
    yy2 = yy1 + yy3;

    %figure(12)
    %surf(corrected_error_x);
    %figure(13)
    %surf(corrected_error_y);
    %figure(14)
    %quiver(xx1,yy1,corrected_error_x,corrected_error_y,'r','AutoScaleFactor',1,'ShowArrowHead','on');title('模拟误差矢量图');

    for i = 1:lengthx
        [fitresult1, gof1] = CreateFit(xx1(i, :), yy1(i, :)); %val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
        k1 = fitresult1.p1;
        b1 = fitresult1.p2;
        % k1_array(i)=k1;
        % b1_array(i)=b1;
        [fitresult2, gof2] = CreateFit(xx2(i, :), yy2(i, :)); %val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
        k2 = fitresult2.p1;
        b2 = fitresult2.p2;
        %k2_array(i)=k2;
        theta(i) = atan(((k2 - k1) / (1 + k1 * k2))) * 180 / pi; % % %每行的夹角
    end

    mean_theta = mean(theta);
    %%%角度校准
    sita = mean_theta * pi / 180;
    Tj = [cos(sita) -1 * sin(sita) 0
          sin(sita) cos(sita) 0
          0 0 1];

    for i = 1:1:lengthx

        for j = 1:1:lengthy
            temp = Tj * [xx1(i, j) yy1(i, j) 1]';
            xx1(i, j) = temp(1);
            yy1(i, j) = temp(2);
        end

    end

    for i = 1:lengthx
        [fitresult1, gof1] = CreateFit(xx1(:, i), yy1(:, i)); %val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
        k1 = fitresult1.p1;
        b1 = fitresult1.p2;
        % k1_array(i)=k1;
        % b1_array(i)=b1;
        [fitresult2, gof2] = CreateFit(xx2(:, i), yy2(:, i)); %val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
        k2 = fitresult2.p1;
        b2 = fitresult2.p2;

        theta(i) = atan(((k2 - k1) / (1 + k1 * k2))) * 180 / pi; % % %每列的夹角
    end

    mean_theta1 = mean(theta) * pi / 180;

    for i = 1:lengthx

        for j = 1:lengthy
            map(i, j) =- (yy1(i, j) - yy1(1, j)) * tan(mean_theta1);
        end

    end

    corrected_error_x = zeros(lengthx, lengthy);
    corrected_error_y = zeros(lengthx, lengthy);
    xx1 = xx1 + map;

    %%%缩放校准系数
    for i = 1:lengthx
        [fitresult3, gof3] = CreateFit(xx1(i, :), xx2(i, :)); %val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
        k3(i) = fitresult3.p1;
        b3 = fitresult3.p2;
    end

    mean_kpx = mean(k3);

    for i = 1:lengthy
        [fitresult4, gof4] = CreateFit(yy1(:, i), yy2(:, i)); %val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
        k4(i) = fitresult4.p1;
        b4 = fitresult4.p2;
    end

    mean_kpy = mean(k4);

    % for i=1:lengthy
    %   [fitresult2, gof2] = CreateFit(yy1(i,:), yy2(i,:));%val(x,y) = p00 + p10*x + p01*y + p20*x^2 + p11*x*y + p02*y^2
    %   k2(i)=fitresult2.p1;
    %   b2=fitresult2.p2;
    % end
    % mean_kpy1=mean(k2);
    %%缩放校准
    Ts = [mean_kpx 0 0
          0 mean_kpy 0
          0 0 1];

    for i = 1:1:lengthx

        for j = 1:1:lengthy
            temp = Ts * [xx1(i, j) yy1(i, j) 1]';
            xx1(i, j) = temp(1);
            yy1(i, j) = temp(2);
        end

    end

    for i = 1:lengthx

        for j = 1:lengthy
            corrected_error_x(i, j) = xx2(i, j) - xx1(i, j);
            corrected_error_y(i, j) = yy2(i, j) - yy1(i, j);
        end

    end

    %平移坐标修正
    tx = sum(corrected_error_x(:)) / (lengthx * lengthy);
    ty = sum(corrected_error_y(:)) / (lengthx * lengthy);
    corrected_error_x = corrected_error_x - tx;
    corrected_error_y = corrected_error_y - ty;

    %figure(12)
    %surf(corrected_error_x);
    %figure(13)
    %surf(corrected_error_y);
    %figure(14)
    %quiver(xx1,yy1,corrected_error_x,corrected_error_y,'r','AutoScaleFactor',1,'ShowArrowHead','on');title('模拟误差矢量图');

    for i = 1:lengthx
        p = polyfit(1:lengthy, corrected_error_x(i, :), 5);
        % poly2str(p,'x');
        corrected_error_x(i, :) = polyval(p, 1:lengthy);
    end

    for i = 1:lengthx
        p = polyfit(1:lengthy, corrected_error_y(i, :), 5);
        corrected_error_y(i, :) = polyval(p, 1:lengthy);
    end

end
