@echo off
netsh interface set interface "Ethernet" disable
netsh interface set interface "Ethernet 3" enable
exit