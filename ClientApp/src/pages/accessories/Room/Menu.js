import React, { useState, createRef } from "react";
import { Modal } from "react-bootstrap";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faBars, faTools, faMicrophone, faUserFriends, faVolumeMute, faVolumeUp } from "@fortawesome/free-solid-svg-icons";
import LocalizedStrings from "react-localization";

const text = new LocalizedStrings({
    en: {
        text: "Register to Enable",
        supportAlert: "Sorry, your browser is not supported.",
        mediaSupport: "Something went wrong. Check your microphone and the permissions.",
        admin: "Admin Tools",
        min: "min",
        hour: "hour",
        hours: "hours",
        mute: "Mute",
        ban: "Ban",
        from: "From",
        till: "Till",
        clear: "Clear Messages",
        wrongTiming: "Wrong timing!",
        confirm: "Are you sure you want to delete all messages?"
    },
    ru: {
        text: "Регистрируйтесь, чтобы Включить",
        supportAlert: "Извините, ваш браузер не поддерживается.",
        mediaSupport: "Что-то пошло не так. Проверьте свой микрофон и разрешения.",
        admin: "Админ. Инструменты",
        min: "мин",
        hour: "час",
        hours: "часов",
        mute: "Заглушить",
        ban: "Забанить",
        from: "От",
        till: "До",
        clear: "Почистить Сообщения",
        wrongTiming: "Неправильное время!",
        confirm: "Вы уверены, что хотите удалить все сообщения?"
    }
});
const voiceSupport = 'RTCPeerConnection' in window && 'mediaDevices' in navigator;

export function Menu(props) {
    const [showModal, setShowModal] = useState(false);
    const [penaltyMins, setPenaltyMins] = useState(2);
    const [datetimeFrom, setDatetimeFrom] = useState("");
    const [datetimeTill, setDatetimeTill] = useState("");
    const userRef = createRef();
    const users = props.users;
    users.sort(function (a, b) {
        if (a.name < b.name)
            return -1;
        if (a.name > b.name)
            return 1;
        return 0;
    });
    const formUser = user => {
        let selected = !props.public && props.selusers.includes(user);
        return <div className={`p-2 pl-3 pr-3${user.guid ? " text-muted" : props.registered ? ` user${selected ? " selected" : ""}` : ""}`}
            key={user.guid ? user.guid : user.id} onClick={!user.guid && props.registered ? () => props.userClicked(user) : null}>
            <img src={`/img/${user.icon}.${user.guid ? "m" : selected ? "light" : "dark"}.svg`} draggable={false}
                className="mr-3 rounded-circle" alt="icon" />
            <span>{user.name}</span>
        </div>
    }
    const setPublic = () => {
        if (!props.public) props.setPublic(true);
        else if (props.selusers.length > 0) props.setPublic(false);
    }
    const voiceClick = () => {
        if (!voiceSupport) alert(text.supportAlert);
        else {
            if (props.voiceActive)
                props.voicButtonClick(null);
            else {
                navigator.mediaDevices.getUserMedia({ video: false, audio: true })
                    .then(stream => {
                        props.voicButtonClick(stream);
                    }).catch(() => {
                        alert(text.mediaSupport);
                    })
            }
        }
    }
    const showAdmin = () => setShowModal(true);
    const onHide = () => {
        setShowModal(false);
    }
    const datetimeFromChanged = ev => {
        setDatetimeFrom(ev.target.value);
    }
    const datetimeTillChanged = ev => {
        setDatetimeTill(ev.target.value);
    }
    const faultTimeChanged = ev => {
        setPenaltyMins(ev.target.value);
    }
    const muteUser = () => {
        let value = userRef.current.value;
        if (!value) return;
        let user = users.find(user => user.id == value || user.guid == value);
        if (user){
            props.muteUser(user, penaltyMins);
            setShowModal(false);
        }
    }
    const banUser = () => {
        let value = userRef.current.value;
        if (!value) return;
        let user = users.find(user => user.id == value || user.guid == value);
        if (user) {
            props.banUser(user, penaltyMins);
            setShowModal(false);
        }
    }
    const clearMessages = () => {
        let from = datetimeFrom ? (new Date(datetimeFrom)).getTime() * 10000 + 621355968000000000 : datetimeFrom;
        let till = datetimeTill ? (new Date(datetimeTill)).getTime() * 10000 + 621355968000000000 : datetimeTill;
        if (from && till && from >= till) alert(text.wrongTiming);
        else if (!from && !till) {
            if (window.confirm(text.confirm)) {
                props.clearMessages(from, till);
                setShowModal(false);
            }
        }
        else {
            props.clearMessages(from, till);
            setShowModal(false);
        }
    }
    text.setLanguage(props.lang);
    return <div id="roommenu" ref={props.menu} tabIndex={-1} className={`bg-dark${props.open ? " menuopen" : ""}`} onBlur={props.closemenu}>
        <nav className="navbar navbar-expand bg-dark navbar-dark">
            <button onClick={props.closemenu} className="btnmenu btn btn-outline-light mr-3"><FontAwesomeIcon icon={faBars} /></button>
            <img src={`/img/${props.icon}.dark.svg`} draggable={false}
                className="mr-3 rounded-circle" alt="icon" />
            <span className={`navbar-brand${props.registered ? "" : " text-muted"}`}>{props.name}</span>
        </nav>
        <div id="names" className="text-light">
            {
                users.map(user => formUser(user))
            }
        </div>
        <div id="menubtns" className="row">
            <div className={`col btn btn-${props.voiceActive ? "danger" : "dark"}`} onClick={voiceClick}>
                <FontAwesomeIcon size="2x" color="#f8f9fa" icon={faMicrophone} /> {props.voiceOnline}
            </div>
            {
                props.registered
                    ? <div className={`col btn btn-dark${props.public ? "" : " active"}`}
                        onClick={setPublic}>
                        <FontAwesomeIcon size="2x" color="#f8f9fa" icon={faUserFriends} />
                    </div>
                    : <div className="col text-warning p-2 text-center" style={{ fontSize: ".8rem" }}>
                        {text.text}
                    </div>
            }
            <div className={`col btn btn-dark${props.sound ? " active" : ""}`} onClick={props.soundClicked}>
                <FontAwesomeIcon size="2x" color="#f8f9fa" icon={props.sound ? faVolumeUp : faVolumeMute} />
            </div>
            {
                props.isAdmin &&
                <>
                    <div className="col btn btn-info" onClick={showAdmin}>
                        <FontAwesomeIcon size="2x" color="#f8f9fa" icon={faTools} />
                    </div>
                    <Modal id="admin_toolbox" show={showModal} onHide={onHide}>
                        <Modal.Header closeButton>
                            <Modal.Title>{text.admin}</Modal.Title>
                        </Modal.Header>
                        <Modal.Body>
                            <div className="form-group input-group">
                                <select className="form-control" ref={userRef}>
                                    {
                                        users.map(user => <option key={`${user.id ? user.id : user.guid}`} value={`${user.id ? user.id : user.guid}`}>{user.name}</option>)
                                    }
                                </select>
                                <div className="input-group-append ml-2">
                                    <select value={penaltyMins} onChange={faultTimeChanged}>
                                        <option value="2">2 {text.min}</option>
                                        <option value="15">15 {text.min}</option>
                                        <option value="30">30 {text.min}</option>
                                        <option value="60">1 {text.hour}</option>
                                        <option value="120">2 {text.hours}</option>
                                    </select>
                                </div>
                            </div>
                            <div className="form-group row">
                                <button className="btn btn-warning col ml-2 mr-2" onClick={muteUser}>{text.mute}</button>
                                <button className="btn btn-danger col ml-2 mr-2" onClick={banUser}>{text.ban}</button>
                            </div>
                            <hr />
                            <div className="form-group input-group">
                                <div className="input-group-prepend">
                                    <span className="input-group-text">{text.from}</span>
                                </div>
                                <input type="datetime-local" min="2020-01-01T12:00:00" value={datetimeFrom} className="form-control" onChange={datetimeFromChanged} />
                            </div>
                            <div className="form-group input-group">
                                <div className="input-group-prepend">
                                    <span className="input-group-text">{text.till}</span>
                                </div>
                                <input type="datetime-local" min="2020-01-01T12:00:00" value={datetimeTill} className="form-control" onChange={datetimeTillChanged} />
                            </div>
                            <div className="form-group">
                                <button className="btn btn-outline-warning btn-block" onClick={clearMessages}>{text.clear}</button>
                            </div>
                        </Modal.Body>
                    </Modal>
                </>
            }
        </div>
    </div>
}