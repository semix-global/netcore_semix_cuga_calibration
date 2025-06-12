a = [1, 3, 6, 8, 9, 8, 6, 3, 1, 0, 1, 3, 6];

% 极值点（一阶导数变号）
diff1 = diff(a);
sign_changes_1st = diff(sign(diff1));
extrema_indices = find(sign_changes_1st ~= 0) + 1;
disp('极值点：');
disp([extrema_indices; a(extrema_indices)]');

% 拐点（二阶导数变号）
diff2 = diff(a, 2);
sign_changes_2nd = diff(sign(diff2));
inflection_indices = find(sign_changes_2nd ~= 0) + 1;
disp('拐点：');
disp([inflection_indices; a(inflection_indices)]');

% 画图将拐点和极值点显示
figure;
plot(a, '-o', 'DisplayName', '数据');
hold on;
plot(extrema_indices, a(extrema_indices), 'r*', 'DisplayName', '极值点');
plot(inflection_indices, a(inflection_indices), 'g*', 'DisplayName', '拐点');
legend show;
title('极值点和拐点');
xlabel('索引');
ylabel('值');
grid on;
% 显示图形
hold off;
