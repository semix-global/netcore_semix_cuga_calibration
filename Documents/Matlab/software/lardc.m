% step 1: transfer function descrete.
% step 2: assemble all

clear
clc

%% Set sample time and end time, note: shanno sampling theory
sampleTime = 0.001;
offset = 0;

% 1.0
endtime = 1.5;
len = endtime / sampleTime;
t = sampleTime:sampleTime:endtime;

%% Parameter

%%% 0.019   15   180
%%% 0.05    15   90

% b0 = 0.019;      % bo greater than or equal to b
% omega_c = 15;
% omega_o = 187.6250;

b0 = 0.019;      % bo greater than or equal to b
omega_c = 20;
omega_o = 187;

Kp = omega_c.^2;
Kd = 2*omega_c;

%% Set input signal
switch(1)
    case 1
        stepTime = 0.1;
        Temp = stepTime / sampleTime;
        input_signal(1:Temp) = 0;
        input_signal(Temp:len+1) = 1;
    case 2
        input_signal = impluse();
    case 3
        input_signal = wave();
    case 4
        input_signal = userDefine();
end

%% Descrete transfer function
% G(s) = 1/(s^2 + 2*s + 4);
% Gz = c2d(G, sampleTime) -----> Y/U = (4.997e-07 z + 4.993e-07) / (z^2 - 1.998 z + 0.998)
% Z^(-1) --->  y(k+2) - 1.998*y(k+1) + 0.998*y(k) = 4.997e-7*u(k+1) + 4.993e-7*u(k)
%        --->  y(k) = 1.998*y(k-1) - 0.998*y(k-2) + 4.997e-7*u(k-1) + 4.993e-7*u(k-2)

%% Initialize all
u0 = zeros(1, len);

z1 = zeros(1, len);
z2 = zeros(1, len);
z3 = zeros(1, len);

e1 = zeros(1, len);
e2 = zeros(1, len);

% Up to Gp(s) transfer function.
% y(k) = -1.998*y(k-1) - 0.998*y(k-2) + 4.997e-7*u(k) + 4.993e-7*u(k - 1)
y = zeros(1, len);
u = zeros(1, len);

%% Initialize all
e1(1) = input_signal(1) - z1(1);
u0(1) = Kp*e1(1) - Kd*z2(1);
e2(1) = u0(1) - z3(1);

u(1) = 0;
u(2) = e2(1) / b0;

% G(s) = 1/(s^2 + 2*s + 4);
% Gz = c2d(G, sampleTime) -----> Y/U = (4.997e-07 z + 4.993e-07) / (z^2 - 1.998 z + 0.998)
y(3) = 1.998*y(2) - 0.998*y(1) + 4.997e-7*u(2) + 4.993e-7*u(1);

% Future z(i-1)   now z(i-2)   before  z(i-3)
z1(2) = z1(1) + sampleTime*(3*omega_o*(y(3) - z1(1)) + z2(1));
z2(2) = z2(1) + sampleTime*(3*omega_o^2*(y(3) - z1(1)) + b0*u(2) + z3(1));
z3(2) = z3(1) + sampleTime*(omega_o^3*(y(3) - z1(1)));

error = 0;
%% Pro proccessing and update
for i = 99:len-1
    e1(i+1) = input_signal(i+1) - z1(i+1);
    u0(i+1) = Kp*e1(i+1) - Kd*z2(i+1);
    e2(i+1) = u0(i+1) - z3(i+1);

    error = error + abs(e1(i+1));
    u(i+2) = e2(i+1) / b0;
    
    % G(s) = 1/(s^2 + 2*s + 4);
    % Gz = c2d(G, sampleTime) -----> Y/U = (4.997e-07 z + 4.993e-07) / (z^2 - 1.998 z + 0.998)
    y(i+3) = 1.998*y(i+2) - 0.998*y(i+2) + 4.997e-7*u(i+2) + 4.993e-7*u(i+1);
    
    % Future z(i-1)   now z(i-2)   before  z(i-3)
    z1(i+2) = z1(i+1) + sampleTime*(3*omega_o*(y(i+3) - z1(i+1)) + z2(i+1));
    z2(i+2) = z2(i+1) + sampleTime*(3*omega_o^2*(y(i+3) - z1(i+1)) + b0*u(i+2) + z3(i+1));
    z3(i+2) = z3(i+1) + sampleTime*(omega_o^3*(y(i+3) - z1(i+1)));
    
    y(i+1) = y(i+2);
    y(i+2) = y(i+3);
    
    z1(i+1) = z1(i+2);
    z2(i+1) = z2(i+2);
    z3(i+1) = z3(i+2);
    
    u(i+1) = u(i+2);

end

error = error / length(u);

% input_signal = input_signal(1:len);
% u = u(1:len);
% y = y(1:len);
% z1 = z1(1:len);
% z2 = z2(1:len);
% z3 = z3(1:len);

%%
subplot(311)
plot(t, input_signal(1:len), t, y(1:len), t, z1(1:len),'--');
xlabel('Time array');
ylabel('set and y');
legend('set', 'y', 'z1')
subplot(312)
plot(t, z2(1:len))
xlabel('Time array');
ylabel('z2 Value');
legend('z2');
subplot(313)
plot(t, z3(1:len))
xlabel('Time array');
ylabel('z3 Value');
legend('z3');

log((1.4-1.312*2^0.026)/(0.002*2^0.48*log(2-0.452*2^1.22)))
