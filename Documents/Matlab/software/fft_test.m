% function ReadReverse()
FFT_N=8;

nv2=FFT_N/2;                  %%//变址运算，即把自然顺序变成倒位序，采用雷德算法 
nm1=FFT_N-1; 
i=1;j=FFT_N/2;
xin=0:1:FFT_N-1;
for i=1:1:nm1            
	if i<j                    %%//如果i<j,即进行变址     
      		t=xin(j+1);                  
     		xin(j+1)=xin(i+1);      
     		xin(i+1)=t;    
    end
   	 k=nv2;                    %%//求j的下一个倒位序 
   	 while(k<=j)               %%//如果k<=j,表示j的最高位为1     	      
     		j=j-k;                 %%//把最高位变成0 
      		k=k/2;                 %%//k/2，比较次高位，依次类推，逐个比较，直到某个位为0      
     end
  	 j=j+k;                   %%//把0改为1  
end

for i=0:FFT_N-1
   W(i+1)=complex(cos(2*pi/FFT_N*i),-1*sin(2*pi/FFT_N*i)) ;
end

x=0:1:FFT_N-1;
for i=0:1:log(FFT_N)/log(2)-1
    if i==0
    l=1;    
    else
    l=i*2;
    end
    for j=0:2*l:FFT_N-1
       for k=0:1:l-1
          product=x(j+k+l+1)*W(FFT_N*k/2/l+1);
          up= x(j+k+1) + product;
		  down= x(j+k+1) - product;
		   x(j+k+1)   = up;
			x(j+k+2) = down;
       end
    end
end