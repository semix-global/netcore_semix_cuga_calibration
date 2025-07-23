
% lvdt_data=xlsread('5XAF_DATA.xlsx','A:A');
% lvdt_data_s=smooth(lvdt_data,90 ,'sgolay',4 );
% plot(lvdt_data_s)

reviewer_data=xlsread('5XAF_DATA.xlsx','B:B');
reviewer_data_s=smooth(reviewer_data,50 ,'sgolay',4 );
plot(reviewer_data_s)

reviewer_data1=ones(15360,1);
Y = fftshift(fft(reviewer_data));
plot(abs(Y(7681:8000)))
Y_abs=abs(Y);
Y_abs_s=smooth(Y_abs,20 ,'sgolay',3 );
plot(abs(Y_abs_s))

%%%设定采样周期是1ms
dt=1*0.001;
df=1/(15360*dt); 
f1=(7758-7681)*df;T1=1/f1/dt; 
f2=(7836-7681)*df;T2=1/f2/dt;
f3=(7923-7681)*df;T3=1/f3/dt;
f4=(8374-7681)*df;T4=1/f4/dt;
f5=(8627-7681)*df;T5=1/f5/dt;
