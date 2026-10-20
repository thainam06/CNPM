import { createContext, useContext } from 'react';
import { portalText } from './portalMessages';
export const PortalLocale = createContext('vi');
export const usePortalText = () => {
  const language = useContext(PortalLocale);
  return text => portalText(language, text);
};
