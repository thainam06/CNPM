export function deviceDescriptor(ua = '') {
  const browser = /Edg\//.test(ua) ? 'Edge' : /OPR\/|Opera/.test(ua) ? 'Opera'
    : /Firefox\/|FxiOS\//.test(ua) ? 'Firefox' : /Chrome\/|CriOS\//.test(ua) ? 'Chrome' : /Safari\//.test(ua) ? 'Safari' : 'Other';
  const operatingSystem = /Android/.test(ua) ? 'Android' : /iPhone|iPad|iPod/.test(ua) ? 'iOS'
    : /Windows/.test(ua) ? 'Windows' : /Macintosh|Mac OS X/.test(ua) ? 'macOS' : /Linux/.test(ua) ? 'Linux' : 'Other';
  const type = /iPad|Tablet/.test(ua) || (operatingSystem === 'Android' && !/Mobile/.test(ua)) ? 'Tablet'
    : /Mobile|iPhone|iPod/.test(ua) ? 'Mobile' : ['Windows', 'macOS', 'Linux'].includes(operatingSystem) ? 'Desktop' : 'Unknown';
  return { browser, operatingSystem, type };
}
