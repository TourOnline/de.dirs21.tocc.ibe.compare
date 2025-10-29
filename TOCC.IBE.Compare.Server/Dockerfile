FROM mcr.microsoft.com/dotnet/aspnet:6.0

RUN apt update
RUN apt install -y libc6-dev
RUN apt install -y libgdiplus

RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/' /etc/ssl/openssl.cnf \
    && sed -i 's/CipherString = DEFAULT@SECLEVEL=2/CipherString = DEFAULT@SECLEVEL=1/' /etc/ssl/openssl.cnf

RUN unlink /etc/localtime
RUN ln -s /usr/share/zoneinfo/Europe/Berlin /etc/localtime

WORKDIR /app
COPY . .
EXPOSE 80
ENTRYPOINT ["dotnet", "TOCC.IBE.Compare.Server.dll"]

