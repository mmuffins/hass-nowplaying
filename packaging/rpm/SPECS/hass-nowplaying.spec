Name: hass-nowplaying
Version: 1.0
Release: 1%{?dist}
Summary: Allows control of Home Assistant media players.
License: MIT
Source0: %{name}-%{version}.tar.gz
BuildArch: x86_64

%description
Controls Home Assistant media players.

%prep
%setup -q

%build

%install
echo "update /usr/bin with % {_bindir}"
rm -rf $RPM_BUILD_ROOT
mkdir -p $RPM_BUILD_ROOT/usr/bin
mkdir -p $RPM_BUILD_ROOT/usr/share/doc/%{name}
install -m 755 %{_sourcedir}/usr/bin/hass-nowplaying $RPM_BUILD_ROOT/usr/bin/
install -m 644 %{_sourcedir}/usr/share/doc/%{name}/README.md $RPM_BUILD_ROOT/usr/share/doc/%{name}/
install -m 644 %{_sourcedir}/usr/share/doc/%{name}/LICENSE $RPM_BUILD_ROOT/usr/share/doc/%{name}/

%files
%attr(0755, root, root) /usr/bin/hass-nowplaying
%attr(0644, root, root) /usr/share/doc/%{name}/README.md
%attr(0644, root, root) /usr/share/doc/%{name}/LICENSE

%post
echo "To configure the application, copy the included example configuration file to your .config directory:"
echo "mkdir -p ~/.config/hass-nowplaying/"
echo "cp /usr/share/doc/hass-nowplaying/appsettings.json ~/.config/hass-nowplaying/"

%preun
if [ $1 -eq 0 ] ; then
    systemctl --user stop hass-nowplaying.service
    systemctl --user disable hass-nowplaying.service
fi

%postun
if [ $1 -eq 0 ] ; then
    echo "hass-nowplaying removed"
fi

%changelog
* Tue Aug 17 2024 muffins <mail@tiv73.com> - 1.0-1
- Initial RPM release
